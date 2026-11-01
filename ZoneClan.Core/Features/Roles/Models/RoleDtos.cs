namespace ZoneClan.Core.Features.Roles.Models;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#57534e";
}

public class UpsertRoleRequest
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#57534e";
}

public class SetPlayerRolesRequest
{
    public List<int> RoleIds { get; set; } = [];
}
