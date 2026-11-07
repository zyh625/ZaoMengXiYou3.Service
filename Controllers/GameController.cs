using Microsoft.AspNetCore.Mvc;

namespace ZaoMengXiYou3.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "online",
            serverTime = DateTimeOffset.UtcNow
        });
    }
}