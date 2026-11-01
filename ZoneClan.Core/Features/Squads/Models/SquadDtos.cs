using ZoneClan.Core.Features.Roles.Models;

namespace ZoneClan.Core.Features.Squads.Models;

public class SquadMemberDto
{
    public int Id { get; set; }
    public ulong PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public string Role { get; set; } = "fighter";
    public string ExtraRole { get; set; } = "";
    public List<RoleDto> Roles { get; set; } = [];
}

public class SquadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SquadMemberDto> Members { get; set; } = [];
}

public class UpdateSquadRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class SquadBuildDto
{
    public int Id { get; set; }
    public int SquadId { get; set; }
    public string Description { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

public class AddSquadMemberRequest
{
    public ulong PlayerId { get; set; }
    public string Role { get; set; } = "fighter";
}

public class UpdateSquadMemberRequest
{
    public string Role { get; set; } = "fighter";
}
