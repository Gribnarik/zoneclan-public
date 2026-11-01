using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Server.Features.Settings.Data;

public class ClanSettingsStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<ClanSettingsDto> GetAsync(ulong guildId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var profile = await connection.QueryFirstOrDefaultAsync<ClanSettingsDto>(
            """
            SELECT guild_id AS GuildId,
                   clan_name AS ClanName,
                   clan_tag AS ClanTag,
                   IFNULL(clan_description, '') AS ClanDescription,
                   clan_role_id AS ClanRoleId,
                   rank_new_role_id AS RankNewRoleId,
                   rank_mid_role_id AS RankMidRoleId,
                   rank_veteran_role_id AS RankVeteranRoleId,
                   recruiter_role_id AS RecruiterRoleId,
                   duelist_role_id AS DuelistRoleId,
                   caller_role_id AS CallerRoleId,
                   timekeeper_role_id AS TimekeeperRoleId,
                   reminder_channel_id AS ReminderChannelId,
                   gathering_channel_id AS GatheringChannelId,
                   penalty_channel_id AS PenaltyChannelId,
                   absent_channel_id AS AbsentChannelId
            FROM clan_settings
            WHERE guild_id = @GuildId
            """,
            new { GuildId = guildId });

        return profile ?? new ClanSettingsDto { GuildId = guildId };
    }

    public async Task UpdateProfileAsync(ulong guildId, ClanProfileUpdate request)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await EnsureGuildRowAsync(connection, guildId, request.UpdatedBy);
        await connection.ExecuteAsync(
            """
            UPDATE clan_settings
            SET clan_name = @Name, clan_tag = @Tag, clan_description = @Description, updated_by = @UpdatedBy
            WHERE guild_id = @GuildId
            """,
            new
            {
                GuildId = guildId,
                Name = request.ClanName.Trim(),
                Tag = request.ClanTag.Trim(),
                Description = request.ClanDescription.Trim(),
                request.UpdatedBy
            });
    }

    public async Task UpdateRoleAsync(ulong guildId, string column, ulong? roleId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await EnsureGuildRowAsync(connection, guildId, 0);
        await connection.ExecuteAsync(
            $"UPDATE clan_settings SET {column} = @RoleId WHERE guild_id = @GuildId",
            new { GuildId = guildId, RoleId = roleId is 0 or null ? null : roleId });
    }

    public async Task UpdateReminderChannelAsync(ulong guildId, ulong channelId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await EnsureGuildRowAsync(connection, guildId, 0);
        await connection.ExecuteAsync(
            "UPDATE clan_settings SET reminder_channel_id = @ChannelId WHERE guild_id = @GuildId",
            new { GuildId = guildId, ChannelId = channelId == 0 ? (ulong?)null : channelId });
    }

    public async Task UpdateGatheringChannelAsync(ulong guildId, ulong channelId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await EnsureGuildRowAsync(connection, guildId, 0);
        await connection.ExecuteAsync("UPDATE clan_settings SET gathering_channel_id = @ChannelId WHERE guild_id = @GuildId",
            new { GuildId = guildId, ChannelId = channelId == 0 ? (ulong?)null : channelId });
    }

    public async Task UpdatePenaltyChannelAsync(ulong guildId, ulong channelId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await EnsureGuildRowAsync(connection, guildId, 0);
        await connection.ExecuteAsync("UPDATE clan_settings SET penalty_channel_id = @ChannelId WHERE guild_id = @GuildId",
            new { GuildId = guildId, ChannelId = channelId == 0 ? (ulong?)null : channelId });
    }

    public async Task UpdateAbsentChannelAsync(ulong guildId, ulong channelId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await EnsureGuildRowAsync(connection, guildId, 0);
        await connection.ExecuteAsync("UPDATE clan_settings SET absent_channel_id = @ChannelId WHERE guild_id = @GuildId",
            new { GuildId = guildId, ChannelId = channelId == 0 ? (ulong?)null : channelId });
    }

    private static async Task EnsureGuildRowAsync(MySqlConnection connection, ulong guildId, ulong updatedBy)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO clan_settings (guild_id, updated_by)
            VALUES (@GuildId, @UpdatedBy)
            ON DUPLICATE KEY UPDATE guild_id = guild_id
            """,
            new { GuildId = guildId, UpdatedBy = updatedBy });
    }
}
