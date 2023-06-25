using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    // GET: api/<LoginController>
    [HttpGet("callback")]
    public async Task<ActionResult<object>> Get([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new string[] { "error", error ?? "No code was provided" };
        }
        return new string[] { "value1", "value2" };
    }
}
