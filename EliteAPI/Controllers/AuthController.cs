using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.AuthService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(
	IAuthService authService, 
	UserManager<ApiUser> userManager) 
	: ControllerBase 
{
	
	[HttpPost]
	[Route("Login")]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> Authenticate([FromBody] DiscordLoginDto credential) {
		var user = await authService.LoginAsync(credential);
		
		if (user is null) {
			return Unauthorized();
		}

		return Ok(user);
	}
	
	[HttpPost]
	[Route("Refresh")]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> RefreshToken([FromBody] AuthResponseDto request) {
		var response = await authService.VerifyRefreshToken(request);
		
		if (response is null) {
			return Unauthorized();
		}

		return Ok(response);
	}

	[Authorize("Admin")]
	[HttpGet("user")]
	public async Task<IActionResult> GetUser() {
		var user = await userManager.GetUserAsync(User);
		if (user is null) return Unauthorized();
		
		return Ok("Hello, " + user.UserName);
	}
}

public class Credential {
	public required string Username { get; set; }
	public required string Password { get; set; }
}