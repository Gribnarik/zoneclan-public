using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Attendance.Shared;
using ZoneClan.Core.Features.Attendance.TabsList.Models;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;

namespace ZoneClan.Server.Features.Attendance.TabsList.Data;

public class WeekDeskStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<WeekDeskDto> GetAsync(DateOnly weekStart, IReadOnlyList<KnownPlayer> roster)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var tabs = (await connection.QueryAsync<TabRow>(
            """
            SELECT id AS Id, stage AS Stage, team1_name AS Team1Name, team1_score AS Team1Score,
                   team2_name AS Team2Name, team2_score AS Team2Score, is_valid AS IsValid,
                   guilds AS Guilds, result AS Result
            FROM match_tabs
            WHERE week_start = @Week
            """,
            new { Week = weekStart.ToDateTime(TimeOnly.MinValue) })).ToList();

        var players = tabs.Count == 0
            ? []
            : (await connection.QueryAsync<PlayerRow>(
                """
                SELECT id AS Id, tab_id AS TabId, player_id AS PlayerId, nickname AS Nickname,
                       kills AS Kills, deaths AS Deaths, assists AS Assists, score AS Score,
                       is_excused AS IsExcused
                FROM match_tab_players
                WHERE tab_id IN @Ids
                ORDER BY score DESC
                """,
                new { Ids = tabs.Select(tab => tab.Id).ToArray() })).ToList();

        var stages = new List<StageColumnDto>();
        var attended = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excused = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var summary = new WeekSummaryDto();

        for (var stage = 1; stage <= 3; stage++)
        {
            var tab = tabs.FirstOrDefault(item => item.Stage == stage);
            var column = new StageColumnDto { Stage = stage };
            if (tab is not null)
            {
                var (recognized, isWinner) = ClanMatch.Evaluate(tab.Team1Name, tab.Team1Score, tab.Team2Name, tab.Team2Score);
                column.TabId = tab.Id;
                column.Team1Name = tab.Team1Name;
                column.Team1Score = tab.Team1Score;
                column.Team2Name = tab.Team2Name;
                column.Team2Score = tab.Team2Score;
                // Если в БД уже сохранён результат (win/loss), используем его — так поражение всегда отображается.
                if (!string.IsNullOrEmpty(tab.Result))
                {
                    column.IsValid = true;
                    column.IsWinner = tab.Result == "win";
                }
                else
                {
                    column.IsValid = tab.IsValid && recognized;
                    column.IsWinner = column.IsValid && isWinner;
                }
                column.Guilds = tab.Guilds;
                column.Players = players.Where(row => row.TabId == tab.Id).Select(row => new TabPlayerRowDto
                {
                    Id = row.Id,
                    PlayerId = row.PlayerId,
                    Nickname = row.Nickname,
                    Kills = row.Kills,
                    Deaths = row.Deaths,
                    Assists = row.Assists,
                    Score = row.Score,
                    IsExcused = row.IsExcused,
                    Status = (row.PlayerId is > 0 ? roster.FirstOrDefault(p => p.DiscordId == row.PlayerId.Value)?.Status : (roster.FirstOrDefault(p => string.Equals(p.Nickname.Trim(), row.Nickname.Trim(), StringComparison.OrdinalIgnoreCase))
                        ?? roster.FirstOrDefault(p => p.Nickname.Trim().Contains(row.Nickname.Trim(), StringComparison.OrdinalIgnoreCase) || row.Nickname.Trim().Contains(p.Nickname.Trim(), StringComparison.OrdinalIgnoreCase)))?.Status) ?? ""
                }).ToList();

                // Добавляем игроков, отмеченных как "Был" в матрице отписок, но отсутствующих в табе
                var attendedFromStatus = await connection.QueryAsync<ulong>(
                    """
                    SELECT player_id
                    FROM player_stage_status
                    WHERE week_start = @Week AND stage = @Stage AND status = 'attended'
                    """,
                    new { Week = weekStart.ToDateTime(TimeOnly.MinValue), Stage = stage });
                foreach (var pid in attendedFromStatus)
                {
                    var rp = roster.FirstOrDefault(r => r.DiscordId == pid);
                    if (rp is null) continue;
                    if (column.Players.Any(p => p.PlayerId == pid)) continue;
                    column.Players.Add(new TabPlayerRowDto
                    {
                        PlayerId = pid,
                        Nickname = rp.Nickname,
                        Kills = 0,
                        Deaths = 0,
                        Assists = 0,
                        Score = 0,
                        IsExcused = false,
                        Status = rp.Status
                    });
                }

                var present = column.Players
                    .Where(row => row.PlayerId is not null)
                    .Select(row => row.PlayerId!.Value)
                    .ToHashSet();
                column.Missed = roster
                    .Where(player => !present.Contains(player.DiscordId))
                    .Select(player => new ClanPlayerDto { PlayerId = player.DiscordId, Nickname = player.Nickname })
                    .OrderBy(player => player.Nickname)
                    .ToList();

                if (column.IsValid)
                {
                    if (column.IsWinner) summary.Wins++;
                    else summary.Losses++;
                    foreach (var row in column.Players.Where(row => !row.IsExcused))
                    {
                        summary.Kills += row.Kills;
                        summary.Deaths += row.Deaths;
                    }
                }

                foreach (var row in column.Players)
                {
                    // Сопоставление по ID (если есть) или по нику
                    string nick = row.Nickname.Trim();
                    if (row.PlayerId is > 0)
                    {
                        var rp = roster.FirstOrDefault(r => r.DiscordId == row.PlayerId.Value);
                        if (rp is not null) nick = rp.Nickname.Trim();
                    }
                    if (string.IsNullOrEmpty(nick))
                        continue;
                    if (row.IsExcused) excused.Add(nick);
                    else attended.Add(nick);
                }
            }
            else
            {
                column.Missed = roster
                    .Select(player => new ClanPlayerDto { PlayerId = player.DiscordId, Nickname = player.Nickname })
                    .OrderBy(player => player.Nickname)
                    .ToList();
            }

            stages.Add(column);
        }

        // Добавляем отписки/пропуски из player_stage_status, которых нет в табах
        var stageStatuses = await connection.QueryAsync<(ulong PlayerId, string Status)>(
            """
            SELECT player_id AS PlayerId, status AS Status
            FROM player_stage_status
            WHERE week_start = @Week
            """,
            new { Week = weekStart.ToDateTime(TimeOnly.MinValue) });
        foreach (var ss in stageStatuses)
        {
            var rp = roster.FirstOrDefault(r => r.DiscordId == ss.PlayerId);
            if (rp is null) continue;
            var nick = rp.Nickname.Trim();
            if (ss.Status == "attended") attended.Add(nick);
            else if (ss.Status == "excused") excused.Add(nick);
            // missed не добавляем; missed считается как roster - attended - excused
        }

        excused.ExceptWith(attended);
        var rosterNicks = roster.Select(player => player.Nickname.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        summary.Attended = attended.Count(rosterNicks.Contains);
        summary.Excused = excused.Count(rosterNicks.Contains);
        summary.Missed = Math.Max(0, roster.Count - summary.Attended - summary.Excused);

        return new WeekDeskDto
        {
            WeekStart = weekStart,
            Summary = summary,
            Stages = stages
        };
    }

    public async Task<bool> SetExcuseAsync(int tabId, int rowId, bool isExcused)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync(
            """
            UPDATE match_tab_players
            SET is_excused = @Excused
            WHERE id = @RowId AND tab_id = @TabId
            """,
            new { TabId = tabId, RowId = rowId, Excused = isExcused });
        return updated > 0;
    }

    public async Task<bool> DeleteTabAsync(int tabId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync("DELETE FROM match_tabs WHERE id = @Id", new { Id = tabId });
        return updated > 0;
    }

    private sealed class TabRow
    {
        public int Id { get; set; }
        public int Stage { get; set; }
        public string Team1Name { get; set; } = "";
        public int Team1Score { get; set; }
        public string Team2Name { get; set; } = "";
        public int Team2Score { get; set; }
        public bool IsValid { get; set; }
        public int Guilds { get; set; }
        public string? Result { get; set; }
    }

    private sealed class PlayerRow
    {
        public int Id { get; set; }
        public int TabId { get; set; }
        public ulong? PlayerId { get; set; }
        public string Nickname { get; set; } = "";
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Score { get; set; }
        public bool IsExcused { get; set; }
    }
}