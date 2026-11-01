using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Attendance.ClanStats.Models;
using ZoneClan.Core.Features.Attendance.PlayerStats.Models;
using ZoneClan.Core.Features.Attendance.Shared;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;

namespace ZoneClan.Server.Features.Attendance.ClanStats.Data;

public class AttendanceStatsStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<(List<PlayerListRowDto> Players, ClanStatsDto Clan)> GetAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyList<KnownPlayer> roster)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var tabs = (await connection.QueryAsync<StatTab>(
            """
            SELECT id AS Id, week_start AS WeekStart, stage AS Stage, is_valid AS IsValid,
                   team1_name AS Team1Name, team1_score AS Team1Score,
                   team2_name AS Team2Name, team2_score AS Team2Score,
                   guilds AS Guilds
            FROM match_tabs
            WHERE week_start BETWEEN @From AND @To
            """,
            new { From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MinValue) })).ToList();

        var rows = tabs.Count == 0
            ? []
            : (await connection.QueryAsync<StatRow>(
                """
                SELECT tab_id AS TabId, player_id AS PlayerId, nickname AS Nickname,
                       kills AS Kills, deaths AS Deaths, assists AS Assists, score AS Score,
                       is_excused AS IsExcused
                FROM match_tab_players
                WHERE tab_id IN @Ids
                """,
                new { Ids = tabs.Select(tab => tab.Id).ToArray() })).ToList();

        var valid = tabs.Where(tab => tab.IsValid && ClanMatch.Evaluate(tab.Team1Name, tab.Team1Score, tab.Team2Name, tab.Team2Score).Recognized).ToList();
        var validIds = valid.Select(tab => tab.Id).ToHashSet();
        var clan = new ClanStatsDto { TotalGames = valid.Count };
        foreach (var tab in valid)
        {
            if (ClanMatch.Evaluate(tab.Team1Name, tab.Team1Score, tab.Team2Name, tab.Team2Score).IsWinner)
                clan.Wins++;
            else
                clan.Losses++;
            clan.Guilds += tab.Guilds;
        }

        var byPlayer = new Dictionary<string, PlayerListRowDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var player in roster)
        {
            byPlayer[player.Nickname] = new PlayerListRowDto
            {
                PlayerId = player.DiscordId,
                Nickname = player.Nickname,
                Status = player.Status,
                Characteristic = player.Characteristic
            };
        }

        foreach (var row in rows.Where(row => validIds.Contains(row.TabId)))
        {
            var key = row.PlayerId?.ToString() ?? row.Nickname;
            if (!byPlayer.TryGetValue(key, out var stat))
            {
                if (row.PlayerId is not null && byPlayer.Values.FirstOrDefault(item => item.PlayerId == row.PlayerId) is { } existing)
                {
                    if (string.IsNullOrEmpty(existing.Status))
                        existing.Status = roster.FirstOrDefault(r => r.DiscordId == row.PlayerId)?.Status ?? "";
                    stat = existing;
                }
                else
                {
                    stat = new PlayerListRowDto { PlayerId = row.PlayerId, Nickname = row.Nickname, Status = roster.FirstOrDefault(r => r.DiscordId == row.PlayerId)?.Status ?? roster.FirstOrDefault(r => string.Equals(r.Nickname, row.Nickname, StringComparison.OrdinalIgnoreCase))?.Status ?? "", Characteristic = roster.FirstOrDefault(r => r.DiscordId == row.PlayerId)?.Characteristic ?? roster.FirstOrDefault(r => string.Equals(r.Nickname, row.Nickname, StringComparison.OrdinalIgnoreCase))?.Characteristic ?? 0 };
                    byPlayer[key] = stat;
                }
            }

            var isInClan = (stat.Status == "in_clan") || (roster.FirstOrDefault(r => r.DiscordId == row.PlayerId)?.Status == "in_clan") || (roster.FirstOrDefault(r => string.Equals(r.Nickname, row.Nickname, StringComparison.OrdinalIgnoreCase))?.Status == "in_clan");
            if (row.IsExcused)
                stat.Excused++;
            else
            {
                stat.Attended++;
                stat.Games++;
                stat.Kills += row.Kills;
                stat.Deaths += row.Deaths;
                stat.Assists += row.Assists;
                stat.Score += row.Score;
                if (isInClan)
                {
                    clan.Kills += row.Kills;
                    clan.Deaths += row.Deaths;
                    clan.Assists += row.Assists;
                    clan.Score += row.Score;
                }
            }
        }

        // Учитываем статусы из player_stage_status (отписки/пропуски/был), которых нет в табах
        var tabById = tabs.ToDictionary(t => t.Id);
        var processedPairs = new HashSet<(ulong? PlayerId, int TabId)>();
        foreach (var row in rows.Where(r => validIds.Contains(r.TabId)))
            processedPairs.Add((row.PlayerId, row.TabId));

        var stageStatuses = await connection.QueryAsync<(int TabId, ulong PlayerId, string Status)>(
            """
            SELECT mt.id AS TabId, pss.player_id AS PlayerId, pss.status AS Status
            FROM player_stage_status pss
            JOIN match_tabs mt ON mt.week_start = pss.week_start AND mt.stage = pss.stage
            WHERE mt.week_start BETWEEN @From AND @To
            """,
            new { From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MinValue) });

        foreach (var ss in stageStatuses)
        {
            if (!validIds.Contains(ss.TabId)) continue;
            if (processedPairs.Contains((ss.PlayerId, ss.TabId))) continue;
            var stat = byPlayer.Values.FirstOrDefault(v => v.PlayerId == ss.PlayerId)
                ?? byPlayer.Values.FirstOrDefault(v => string.Equals(v.Nickname, roster.FirstOrDefault(r => r.DiscordId == ss.PlayerId)?.Nickname, StringComparison.OrdinalIgnoreCase));
            if (stat is null) continue;
            if (ss.Status == "attended")
            {
                stat.Attended++;
                stat.Games++;
            }
            else if (ss.Status == "excused")
            {
                stat.Excused++;
            }
        }

        foreach (var stat in byPlayer.Values)
            stat.Missed = Math.Max(0, valid.Count - stat.Attended - stat.Excused);

        // Только игроки в клане
        var players = byPlayer.Values
            .Where(player => string.Equals(player.Status, "in_clan", StringComparison.OrdinalIgnoreCase))
            .OrderBy(player => player.Nickname).ToList();
        clan.TopByKd = Top(players, player => player.Kd);
        clan.TopByKda = Top(players, player => player.Deaths == 0 ? player.Kills + player.Assists : (player.Kills + player.Assists) / (double)player.Deaths);
        clan.TopByScore = Top(players, player => player.Score);
        clan.TopByAttendance = Top(players, player => player.Attended);
        clan.LowByKd = Low(players, player => player.Kd);
        clan.LowByKda = Low(players, player => player.Deaths == 0 ? player.Kills + player.Assists : (player.Kills + player.Assists) / (double)player.Deaths);
        clan.LowByScore = Low(players, player => player.Score);
        clan.LowByAttendance = Low(players, player => player.Attended);
        clan.LowByMissed = Top(players, player => player.Missed);
        return (players, clan);
    }

    private static List<TopPlayerDto> Top(List<PlayerListRowDto> players, Func<PlayerListRowDto, double> value) =>
        players
            .Where(player => player.Games > 0)
            .Select(player => new TopPlayerDto { PlayerId = player.PlayerId, Nickname = player.Nickname, Value = Math.Round(value(player), 2) })
            .OrderByDescending(player => player.Value)
            .Take(5)
            .ToList();

    private static List<TopPlayerDto> Low(List<PlayerListRowDto> players, Func<PlayerListRowDto, double> value) =>
        players
            .Where(player => player.Games > 0)
            .Select(player => new TopPlayerDto { PlayerId = player.PlayerId, Nickname = player.Nickname, Value = Math.Round(value(player), 2) })
            .OrderBy(player => player.Value)
            .Take(5)
            .ToList();

    private sealed class StatTab
    {
        public int Id { get; set; }
        public bool IsValid { get; set; }
        public string Team1Name { get; set; } = "";
        public int Team1Score { get; set; }
        public string Team2Name { get; set; } = "";
        public int Team2Score { get; set; }
        public int Guilds { get; set; }
    }

    private sealed class StatRow
    {
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
