using NetCord;
using NetCord.Gateway;

namespace ZoneClan.Bot.Features.Shared;

internal static class GuildAccess
{
    public static bool CanManageGuild(Guild? guild, User user)
    {
        if (guild is null)
            return false;
        if (guild.OwnerId == user.Id)
            return true;

        var permissions = guild.EveryoneRole?.Permissions ?? default;
        if (user is GuildUser guildUser)
        {
            foreach (var roleId in guildUser.RoleIds)
            {
                if (guild.Roles.TryGetValue(roleId, out var role))
                    permissions |= role.Permissions;
            }
        }

        return permissions.HasFlag(Permissions.Administrator) || permissions.HasFlag(Permissions.ManageGuild);
    }

    public static bool HasStaffRole(Guild? guild, User user, ulong? requiredRoleId)
    {
        if (requiredRoleId is null or 0 || CanManageGuild(guild, user))
            return true;
        if (user is not GuildUser guildUser)
            return false;
        return guildUser.RoleIds.Contains(requiredRoleId.Value);
    }
}
