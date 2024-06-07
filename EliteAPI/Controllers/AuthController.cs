using System.Security.Claims;
using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.AuthService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase 
{
	/// <summary>
	/// Get logged in account
	/// </summary>
	/// <remarks>Used to get session information from the token</remarks>
	/// <returns></returns>
	[Authorize]
	[HttpGet("me")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
	public async Task<ActionResult<AuthSessionDto>> GetSelfOverview()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
		
		if (id is not null && User.AccessTokenExpired()) {
			await authService.TriggerAuthTokenRefresh(id!);
		}
		
		return Ok(new AuthSessionDto {
			Id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
			Username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
			Avatar = User.FindFirstValue(ApiUserClaims.Avatar) ?? string.Empty,
			Ign = User.FindFirstValue(ApiUserClaims.Ign) ?? string.Empty,
			Roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
		});
	}
	
	/// <summary>
	/// Login with Discord credentials
	/// </summary>
	/// <remarks>Used for <see href="https://elitebot.dev/">the website</see> to login users with Discord</remarks>
	/// <param name="credential"></param>
	/// <returns></returns>
	[HttpPost]
	[Route("Login")]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<AuthResponseDto>> Authenticate([FromBody] DiscordLoginDto credential) {
		var user = await authService.LoginAsync(credential);
		
		if (user is null) {
			return Unauthorized();
		}

		return Ok(user);
	}
	
	/// <summary>
	/// Refresh users tokens
	/// </summary>
	/// <remarks>Used for <see href="https://elitebot.dev/">the website</see> to refresh user logins</remarks>
	/// <param name="request"></param>
	/// <returns></returns>
	[HttpPost]
	[Route("Refresh")]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] AuthRefreshDto request) {
		var response = await authService.VerifyRefreshToken(request);
		
		if (response is null) {
			return Unauthorized();
		}

		return Ok(response);
	}
}
