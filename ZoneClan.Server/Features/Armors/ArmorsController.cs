using Microsoft.AspNetCore.Mvc;
using ZoneClan.Server.Features.Armors.List.Data;

namespace ZoneClan.Server.Features.Armors;

[ApiController]
[Route("api/[controller]")]
public class ArmorsController(GetArmorsQuery query) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetArmors() => Ok(await query.ExecuteAsync());
}
