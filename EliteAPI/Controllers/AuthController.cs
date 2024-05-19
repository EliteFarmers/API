using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(IConfiguration configuration) : ControllerBase {
	
	// TODO: Implement authentication logic
	[HttpPost]
	public IActionResult Authenticate([FromBody] Credential credential) {
		if (credential is { Username: "admin", Password: "password" }) {
			var claims = new List<Claim> {
				new Claim(ClaimTypes.Name, credential.Username),
				new Claim(ClaimTypes.Email, "admin@elitebot.dev"),
				new Claim("Admin", "true")
			};
			
			var expiresAt = DateTime.UtcNow.AddMinutes(30);

			return Ok(new {
				access_token = CreateToken(claims, expiresAt),
				expires_at = expiresAt
			});
		}
		
		ModelState.AddModelError("Unauthorized", "Invalid credentials.");
		return Unauthorized(ModelState);
	}

	[Authorize]
	[HttpGet("user")]
	public IActionResult GetUser() {
		return Ok("Hello, " + User.Identity?.Name);
	}

	/// <summary>
	/// Generates a JWT token
	/// </summary>
	/// <param name="claims"></param>
	/// <param name="expiresAt"></param>
	/// <returns></returns>
	private string CreateToken(IEnumerable<Claim> claims, DateTime expiresAt) {
		var secret = configuration["Jwt:Secret"] ?? throw new Exception("Jwt:Secret is not set in app settings");
		
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		
		var token = new JwtSecurityToken(
			claims: claims,
			notBefore: DateTime.UtcNow,
			expires: expiresAt,
			signingCredentials: credentials
		);
		
		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}

public class Credential {
	public required string Username { get; set; }
	public required string Password { get; set; }
}