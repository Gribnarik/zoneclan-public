using Dapper;
using MySqlConnector;
using ZoneClan.Server.Features.Shared;

namespace ZoneClan.Server.Features.Attendance.Data;

public static class AttendanceSchema
{
    public static async Task EnsureCreatedAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await SchemaSql.ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS match_tabs (
              id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
              week_start DATE NOT NULL,
              stage TINYINT NOT NULL,
              image_path VARCHAR(500) NULL,
              team1_name VARCHAR(100) NULL,
              team1_score INT NOT NULL DEFAULT 0,
              team2_name VARCHAR(100) NULL,
              team2_score INT NOT NULL DEFAULT 0,
              guilds INT NOT NULL DEFAULT 0,
              is_valid TINYINT(1) NOT NULL DEFAULT 1,
              created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
              UNIQUE KEY ux_week_stage (week_start, stage)
            )
            """);
        await SchemaSql.AddColumnAsync(connection,
            "ALTER TABLE match_tabs ADD COLUMN guilds INT NOT NULL DEFAULT 0");
        await SchemaSql.ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS match_tab_players (
              id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
              tab_id INT NOT NULL,
              player_id BIGINT NULL,
              nickname VARCHAR(100) NOT NULL,
              kills INT NOT NULL DEFAULT 0,
              deaths INT NOT NULL DEFAULT 0,
              assists INT NOT NULL DEFAULT 0,
              score INT NOT NULL DEFAULT 0,
              is_winner TINYINT(1) NOT NULL DEFAULT 0,
              team_name VARCHAR(100) NULL,
              is_excused TINYINT(1) NOT NULL DEFAULT 0,
              KEY ix_tab (tab_id),
              KEY ix_player (player_id),
              CONSTRAINT fk_match_tab_players_tab FOREIGN KEY (tab_id) REFERENCES match_tabs(id) ON DELETE CASCADE
            )
            """);
        await SchemaSql.ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS player_stage_status (
              week_start DATE NOT NULL,
              stage TINYINT NOT NULL,
              player_id BIGINT NOT NULL,
              status VARCHAR(16) NOT NULL,
              PRIMARY KEY (week_start, stage, player_id),
              KEY ix_player (player_id)
            )
            """);

        await connection.ExecuteAsync(
            """
            INSERT IGNORE INTO player_stage_status (week_start, stage, player_id, status)
            SELECT t.week_start, t.stage, p.player_id, IF(p.is_excused = 1, 'excused', 'attended')
            FROM match_tab_players p
            JOIN match_tabs t ON t.id = p.tab_id
            WHERE p.player_id IS NOT NULL
            """);
    }
}
