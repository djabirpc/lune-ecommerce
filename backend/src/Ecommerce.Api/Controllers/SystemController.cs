using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { success = true, message = "Luna API is running." });
}
