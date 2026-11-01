using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Attendance.Excuses.Models;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;

namespace ZoneClan.Server.Features.Attendance.Excuses.Data;

public class ExcuseStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<ExcuseMatrixDto> GetAsync(DateOnly from, DateOnly to, IReadOnlyList<KnownPlayer> roster)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var tabs = (await connection.QueryAsync<TabRow>(
            """
            SELECT id AS TabId, week_start AS WeekStart, stage AS Stage
            FROM match_tabs
            WHERE week_start BETWEEN @From AND @To
            ORDER BY week_start, stage
            """,
            new { From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MinValue) }))
            .Select(tab => new ExcuseTabDto
            {
                TabId = tab.TabId,
                WeekStart = DateOnly.FromDateTime(tab.WeekStart),
                Stage = tab.Stage
            })
            .ToList();

        var tabIds = tabs.Select(tab => tab.TabId).ToArray();
        var cells = tabs.Count == 0
            ? []
            : (await connection.QueryAsync<CellRow>(
                """
                SELECT tab_id AS TabId, player_id AS PlayerId, is_excused AS IsExcused
                FROM match_tab_players
                WHERE tab_id IN @Ids AND player_id IS NOT NULL
                """,
                new { Ids = tabIds })).ToList();

        // Статусы из player_stage_status (отписки/пропуски), которых нет в табе
        var statusRows = tabs.Count == 0
            ? []
            : (await connection.QueryAsync<(int TabId, ulong PlayerId, string Status)>(
                """
                SELECT mt.id AS TabId, pss.player_id AS PlayerId, pss.status AS Status
                FROM player_stage_status pss
                JOIN match_tabs mt ON mt.week_start = pss.week_start AND mt.stage = pss.stage
                WHERE mt.id IN @Ids
                """,
                new { Ids = tabIds })).ToList();

        var cellList = cells.Select(cell => new ExcuseCellDto
        {
            TabId = cell.TabId,
            PlayerId = cell.PlayerId,
            Status = cell.IsExcused ? "excused" : "attended"
        }).ToList();

        foreach (var sr in statusRows)
        {
            if (!cellList.Any(c => c.TabId == sr.TabId && c.PlayerId == sr.PlayerId))
            {
                cellList.Add(new ExcuseCellDto { TabId = sr.TabId, PlayerId = sr.PlayerId, Status = sr.Status });
            }
            else
            {
                var existing = cellList.First(c => c.TabId == sr.TabId && c.PlayerId == sr.PlayerId);
                if (sr.Status is "excused" or "missed")
                    existing.Status = sr.Status;
            }
        }

        return new ExcuseMatrixDto
        {
            Tabs = tabs,
            Players = roster.Select(player => new ExcusePlayerDto { PlayerId = player.DiscordId, Nickname = player.Nickname }).OrderBy(player => player.Nickname).ToList(),
            Cells = cellList
        };
    }

        public async Task SetAsync(int tabId, SetExcuseRequest request)
    {
        var status = request.Status;
        if (status is not ("attended" or "excused" or "missed"))
            throw new InvalidOperationException("Статус: attended, excused или missed");

        await using var connection = new MySqlConnection(_connectionString);
        
        // Получаем данные таба (какая это неделя и этап)
        var tab = await connection.QueryFirstOrDefaultAsync<(DateTime WeekStart, int Stage)>(
            "SELECT week_start, stage FROM match_tabs WHERE id = @Id", new { Id = tabId });
            
        if (tab.WeekStart == default)
            throw new InvalidOperationException("Таб не найден");

        var nickname = await connection.ExecuteScalarAsync<string?>(
            "SELECT ingame_name FROM players WHERE discord_id = @Id",
            new { Id = request.PlayerId });
        if (string.IsNullOrWhiteSpace(nickname))
            throw new InvalidOperationException("Игрок не найден");

        var rowId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM match_tab_players WHERE tab_id = @TabId AND player_id = @PlayerId",
            new { TabId = tabId, PlayerId = request.PlayerId });

        // 1. Удаляем игрока из таблицы таба, чтобы он не висел с нулями
        if (rowId is not null)
        {
            await connection.ExecuteAsync("DELETE FROM match_tab_players WHERE id = @Id", new { Id = rowId });
        }

        // 2. Записываем статус в player_stage_status (или удаляем, если "Был", так как он и так в табе)
        if (status == "attended")
        {
             await connection.ExecuteAsync(
                """
                INSERT INTO player_stage_status (week_start, stage, player_id, status)
                VALUES (@Week, @Stage, @PlayerId, 'attended')
                ON DUPLICATE KEY UPDATE status = 'attended'
                """,
                new { Week = tab.WeekStart, Stage = tab.Stage, PlayerId = request.PlayerId });
        }
        else if (status == "excused")
        {
             await connection.ExecuteAsync(
                """
                INSERT INTO player_stage_status (week_start, stage, player_id, status)
                VALUES (@Week, @Stage, @PlayerId, 'excused')
                ON DUPLICATE KEY UPDATE status = 'excused'
                """,
                new { Week = tab.WeekStart, Stage = tab.Stage, PlayerId = request.PlayerId });
        }
        else // missed
        {
             await connection.ExecuteAsync(
                """
                INSERT INTO player_stage_status (week_start, stage, player_id, status)
                VALUES (@Week, @Stage, @PlayerId, 'missed')
                ON DUPLICATE KEY UPDATE status = 'missed'
                """,
                new { Week = tab.WeekStart, Stage = tab.Stage, PlayerId = request.PlayerId });
        }
    }

    public async Task SetBulkAsync(BulkExcuseRequest request)
    {
                await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Получаем данные этапа (какая неделя и какой этап)
        var tab = await connection.QueryFirstOrDefaultAsync<(DateTime WeekStart, int Stage)>(
            "SELECT week_start, stage FROM match_tabs WHERE id = @Id", new { Id = request.TabId });

        if (tab.WeekStart == default)
            throw new InvalidOperationException("Таб не найден");

        await using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            foreach (var playerId in request.PlayerIds)
            {
                var nickname = await connection.ExecuteScalarAsync<string?>(
                    "SELECT ingame_name FROM players WHERE discord_id = @Id",
                    new { Id = playerId }, transaction);
                
                if (string.IsNullOrWhiteSpace(nickname)) continue;

                var rowId = await connection.ExecuteScalarAsync<int?>(
                    "SELECT id FROM match_tab_players WHERE tab_id = @TabId AND player_id = @PlayerId",
                    new { TabId = request.TabId, PlayerId = playerId }, transaction);

                // 1. Если игрок есть в таблице табов, но мы ставим пропуск/отписку — удаляем его оттуда
                if (request.Status != "attended" && rowId is not null)
                {
                    await connection.ExecuteAsync(
                        "DELETE FROM match_tab_players WHERE id = @Id", 
                        new { Id = rowId }, transaction);
                }

                // 2. Записываем статус в отдельную таблицу
                await connection.ExecuteAsync(
                    """
                    INSERT INTO player_stage_status (week_start, stage, player_id, status)
                    VALUES (@Week, @Stage, @PlayerId, @Status)
                    ON DUPLICATE KEY UPDATE status = @Status
                    """,
                    new { Week = tab.WeekStart, Stage = tab.Stage, PlayerId = playerId, Status = request.Status },
                    transaction);
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private sealed class TabRow
    {
        public int TabId { get; set; }
        public DateTime WeekStart { get; set; }
        public int Stage { get; set; }
    }

    private sealed class CellRow
    {
        public int TabId { get; set; }
        public ulong PlayerId { get; set; }
        public bool IsExcused { get; set; }
    }
}