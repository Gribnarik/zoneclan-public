using Dapper;
using MySqlConnector;
using ZoneClan.Server.Features.Shared;

namespace ZoneClan.Server.Features.Roles.Data;

public static class RolesSchema
{
    public static async Task EnsureCreatedAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await SchemaSql.ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS roles (
              id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
              name VARCHAR(80) NOT NULL,
              color VARCHAR(16) NOT NULL DEFAULT '#57534e',
              UNIQUE KEY ux_roles_name (name)
            )
            """);
        await SchemaSql.ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS player_roles (
              player_id BIGINT NOT NULL,
              role_id INT NOT NULL,
              PRIMARY KEY (player_id, role_id),
              KEY ix_role (role_id),
              CONSTRAINT fk_player_roles_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
            )
            """);

        var seeds = new (string Name, string Color)[]
        {
            ("Командир отряда", "#9a3412"),
            ("Ответственный за застройку", "#1d4ed8"),
            ("Поиск ралика", "#3f6212")
        };
        foreach (var seed in seeds)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO roles (name, color)
                SELECT @Name, @Color FROM DUAL
                WHERE NOT EXISTS (SELECT 1 FROM roles WHERE name = @Name)
                """,
                new { seed.Name, seed.Color });
        }
    }
}
