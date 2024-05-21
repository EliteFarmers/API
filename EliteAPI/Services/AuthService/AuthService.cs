using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Services.AuthService;

public class AuthService(
	IDiscordService discordService, 
	UserManager<ApiUser> userManager, 
	IConfiguration configuration) 
	: IAuthService 
{
	
	private const string LoginProvider = "EliteAPI";
	private const string RefreshToken = "RefreshToken";
	
	public async Task<AuthResponseDto?> LoginAsync(DiscordLoginDto dto) {
		var account = await discordService.GetDiscordUser(dto.AccessToken);
		
		if (account is null) {
			return null;
		}
		
		var user = await userManager.FindByIdAsync(account.Id.ToString());
		
		// Register the user if they do not exist
		if (user is null) {
			var errors = await RegisterUser(account, dto);
			
			if (errors.Any()) {
				return null;
			}

			user = await userManager.FindByIdAsync(account.Id.ToString());
		}
		
		// If the user is still null, return null (an error occurred)
		if (user is null) {
			return null;
		}
		
		// Update the username if it has changed
		if (user.UserName != account.Username) {
			user.UserName = account.Username;
		}
		
		UpdateUserDiscordTokens(user, dto);
		
		await userManager.UpdateAsync(user);
			
		var (token, expiry) = await GenerateJwtToken(user);
		
		return new AuthResponseDto {
			AccessToken = token,
			ExpiresIn = expiry.ToUnixTimeSeconds().ToString(),
			RefreshToken = await GenerateRefreshToken(user)
		};
	}

	private async Task<string> GenerateRefreshToken(ApiUser user) {
		await userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshToken);
		
		var newRefreshToken = await userManager.GenerateUserTokenAsync(user, LoginProvider, RefreshToken);
		
		var result = await userManager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshToken, newRefreshToken);
		
		if (!result.Succeeded) {
			return string.Empty;
		}

		return newRefreshToken;
	}
	
	public async Task<AuthResponseDto?> VerifyRefreshToken(AuthResponseDto authResponse) {
		var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
		var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(authResponse.AccessToken);
		
		var userId = tokenContent.Claims.ToList()
			.FirstOrDefault(q => q.Type == ClaimTypes.NameIdentifier)?.Value;
		if (userId is null) return null;
		
		var user = await userManager.FindByIdAsync(userId);
		if (user is null) return null;
		
		var isValid = await userManager.VerifyUserTokenAsync(user, LoginProvider, RefreshToken, authResponse.RefreshToken);

		if (!isValid) {
			// Invalidate user tokens
			await userManager.UpdateSecurityStampAsync(user);
			return null;
		}
		
		// Update stored discord tokens
		if (user.DiscordRefreshToken is not null) {
			var response = await discordService.RefreshDiscordUser(user.DiscordRefreshToken);
			
			if (response is not null) {
				user.DiscordAccessToken = response.AccessToken;
				user.DiscordAccessTokenExpires = response.AccessTokenExpires ?? DateTimeOffset.UtcNow.AddMinutes(8);
				user.DiscordRefreshToken = response.RefreshToken;
				user.DiscordRefreshTokenExpires = user.DiscordAccessTokenExpires.AddDays(20);
				
				await userManager.UpdateAsync(user);
			}
		}
		
		var (token, expiry) = await GenerateJwtToken(user);
		return new AuthResponseDto {
			AccessToken = token,
			ExpiresIn = expiry.ToUnixTimeSeconds().ToString(),
			RefreshToken = await GenerateRefreshToken(user)
		};
	}

	public async Task<(string token, DateTimeOffset expiry)> GenerateJwtToken(ApiUser user) {
		var secret = configuration["Jwt:Secret"] ?? throw new Exception("Jwt:Secret is not set in app settings");
		var expiresAt = configuration["Jwt:TokenExpirationInMinutes"] is { } expiration
			? DateTimeOffset.UtcNow.AddMinutes(int.Parse(expiration))
			: DateTimeOffset.UtcNow.AddMinutes(30);
		
		var claims = new List<Claim> {
			new(ClaimTypes.Name, user.UserName ?? string.Empty),
			new(ClaimTypes.NameIdentifier, user.Id),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		};
		
		// Add roles to the claims
		var roles = await userManager.GetRolesAsync(user);
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
	
		var token = new JwtSecurityToken(
			claims: claims,
			notBefore: DateTime.UtcNow,
			expires: expiresAt.DateTime,
			signingCredentials: credentials
		);
	
		return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
	}

	private async Task<IEnumerable<IdentityError>> RegisterUser(EliteAccount account, DiscordLoginDto dto) {
		var user = new ApiUser {
			Id = account.Id.ToString(),
			AccountId = account.Id,
			
			UserName = account.Username,
			Email = account.Email ?? string.Empty,
		};
		
		UpdateUserDiscordTokens(user, dto);
		
		var result = await userManager.CreateAsync(user);

		if (result.Succeeded) {
			await userManager.AddToRoleAsync(user, "User");
		}
		
		return result.Errors;
	}
	
	private static void UpdateUserDiscordTokens(ApiUser user, DiscordLoginDto dto) {
		var accessExpires = long.TryParse(dto.ExpiresIn, out var expiresIn)
			? DateTimeOffset.FromUnixTimeSeconds(expiresIn)
			: DateTimeOffset.UtcNow.AddMinutes(8);

		user.DiscordAccessToken = dto.AccessToken;
		user.DiscordAccessTokenExpires = accessExpires;
		user.DiscordRefreshToken = dto.RefreshToken;
		user.DiscordAccessTokenExpires = accessExpires.AddDays(20);
	}
}