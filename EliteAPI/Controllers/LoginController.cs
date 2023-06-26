using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly IDiscordService _discordService;

    public LoginController(IDiscordService discordService)
    {
        _discordService = discordService;
    }

    // GET: api/<LoginController>
    [HttpGet("callback")]
    public async Task<ActionResult<object>> Get([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(error ?? "No code was provided");
        }

        HttpContext.Request.Cookies.TryGetValue("discord_auth_state", out var authState);
        if (string.IsNullOrWhiteSpace(state) || authState is null || !authState.Equals(state))
        {
            return BadRequest("No state was provided or was invalid");
        }
        HttpContext.Response.Cookies.Delete("discord_auth_state");

        var response = await _discordService.FetchRefreshToken(code);
        if (response is null)
        {
            return BadRequest("Failed to fetch refresh token");
        }

        HttpContext.Response.Cookies.Append("discord_access_token", response.AccessToken, new CookieOptions
        {
            Expires = response.AccessTokenExpires,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        HttpContext.Response.Cookies.Append("discord_refresh_token", response.RefreshToken, new CookieOptions
        {
            Expires = response.RefreshTokenExpires,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        return Ok("Success");
    }
}
