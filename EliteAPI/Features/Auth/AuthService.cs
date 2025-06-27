using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EliteAPI.Background.Discord;
using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace EliteAPI.Features.Auth;

[RegisterService<IAuthService>(LifeTime.Scoped)]
public partial class AuthService(
	IDiscordService discordService, 
	UserManager userManager,
	IConfiguration configuration,
	ISchedulerFactory schedulerFactory,
	DataContext context) 
	: IAuthService 
{
	private const string LoginProvider = "EliteAPI";
	private const string RefreshToken = "RefreshToken";
	
	[GeneratedRegex("^[0-9]$")]
	private static partial Regex DiscordIdRegex();
	
	public async Task<AuthResponseDto?> LoginAsync(DiscordLoginDto dto) {
		var login = await discordService.FetchRefreshToken(dto.Code, dto.RedirectUri);
		if (login is null) {
			return null;
		}
		
		var account = await discordService.GetDiscordUser(login.AccessToken);
		
		if (account is null) {
			return null;
		}
		
		// Register the user if they do not exist
		var user = await userManager.FindByIdAsync(account.Id.ToString());
		if (user is null) 
		{
			var errors = await RegisterUser(account, login);
			if (errors.Any()) return null;
			
			user = await userManager.FindByIdAsync(account.Id.ToString());
			if (user is null) return null; // Should not happen if registration succeeded
		} 
		else 
		{
			// Update existing user if necessary
			if (user.UserName != account.Username) {
				user.UserName = account.Username;
			}
			
			UpdateUserDiscordTokens(user, login);
			await userManager.UpdateAsync(user);
		}

		
		UpdateUserDiscordTokens(user, login);
		
		await userManager.UpdateAsync(user);
			
		var (token, expiry) = await GenerateJwtToken(user);
		var refreshToken = await GenerateAndStoreRefreshToken(user);
		
		if (refreshToken.IsNullOrEmpty()) {
			return null;
		}
		
		return new AuthResponseDto {
			AccessToken = token,
			ExpiresIn = expiry.ToUnixTimeSeconds().ToString(),
			RefreshToken = refreshToken,
		};
	}

	private async Task<string?> GenerateAndStoreRefreshToken(ApiUser user)
	{
		var randomNumber = new byte[64]; // Generate a secure random token
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(randomNumber);
		var refreshTokenValue = Convert.ToBase64String(randomNumber);

		var refreshTokenValidityInDays = configuration.GetValue("Jwt:RefreshTokenExpirationInDays", 30);

		var refreshToken = new RefreshToken
		{
			Token = refreshTokenValue,
			UserId = user.Id,
			CreatedUtc = DateTime.UtcNow,
			ExpiresUtc = DateTime.UtcNow.AddDays(refreshTokenValidityInDays)
		};

		await context.RefreshTokens.AddAsync(refreshToken);
		var saved = await context.SaveChangesAsync();

		return saved > 0 ? refreshTokenValue : null; // Return the token value only if saved successfully
	}

	public async Task<AuthResponseDto?> VerifyRefreshToken(AuthRefreshDto dto) {
		if (DiscordIdRegex().IsMatch(dto.UserId)) {
			return await VerifyRefreshToken(dto.UserId, dto.RefreshToken);
		}
		
		var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
		var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(dto.UserId);
		
		var userId = tokenContent.Claims
			.FirstOrDefault(q => q.Type == ClaimNames.NameId)?.Value;
		
		if (userId is null) return null;

		return await VerifyRefreshToken(userId, dto.RefreshToken);
	}

	public async Task<AuthResponseDto?> VerifyRefreshToken(string userId, string refreshToken) {
		var user = await userManager.FindByIdAsync(userId);
		if (user is null) return null;
		
		var storedToken = await context.RefreshTokens
			.FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);

		if (storedToken is null || !storedToken.IsActive) {
			return null;
		}
		
		var newRefreshTokenValue = await GenerateAndStoreRefreshToken(user);
		if (newRefreshTokenValue.IsNullOrEmpty()) {
			return null;
		}
		
		storedToken.RevokedUtc = DateTime.UtcNow;
		context.RefreshTokens.Update(storedToken);
		await context.SaveChangesAsync();
		
		// Update stored discord tokens
		await discordService.RefreshDiscordUserIfNeeded(user);
		
		var (token, expiry) = await GenerateJwtToken(user);
		return new AuthResponseDto {
			AccessToken = token,
			ExpiresIn = expiry.ToUnixTimeSeconds().ToString(),
			RefreshToken = newRefreshTokenValue
		};
	}

	public async Task<(string token, DateTimeOffset expiry)> GenerateJwtToken(ApiUser user) {
		// Load user accounts (and minecraft accounts)
		await context.Entry(user).Reference(x => x.Account).LoadAsync();
		
		var primaryAccount = user.Account.MinecraftAccounts.FirstOrDefault(q => q.Selected)
			?? user.Account.MinecraftAccounts.FirstOrDefault();
		
		var claims = new List<Claim> {
			new(ClaimNames.Name, user.UserName ?? string.Empty),
			new(ClaimNames.NameId, user.Id),
			new(ClaimNames.Jti, Guid.NewGuid().ToString()),
			new(ClaimNames.Avatar, user.Account.Avatar ?? string.Empty),
			new(ClaimNames.Ign, primaryAccount?.Name ?? string.Empty),
			new(ClaimNames.FormattedIgn, user.Account.GetFormattedIgn()),
			new(ClaimNames.Uuid, primaryAccount?.Id ?? string.Empty),
		};
		
		// Add roles to the claims
		var roles = await userManager.GetRolesAsync(user);
		claims.AddRange(roles.Select(role => new Claim(ClaimNames.Role, role)));

		var expiresAt = configuration["Jwt:TokenExpirationInMinutes"] is { } expiration
			? DateTime.UtcNow.AddMinutes(int.Parse(expiration))
			: DateTime.UtcNow.AddMinutes(30);
		
		var token = JwtBearer.CreateToken(o => {
			o.User.Claims.AddRange(claims);
			o.ExpireAt = expiresAt;
		});

		return (token, expiresAt);
	}

	private async Task<IEnumerable<IdentityError>> RegisterUser(EliteAccount account, DiscordUpdateResponse dto) {
		var user = new ApiUser {
			Id = account.Id.ToString(),
			AccountId = account.Id,
			UserName = account.Username
		};
		
		UpdateUserDiscordTokens(user, dto);
		
		var result = await userManager.CreateAsync(user);

		if (result.Succeeded) {
			await userManager.AddToRoleAsync(user, "User");
		}
		
		return result.Errors;
	}
	
	private static void UpdateUserDiscordTokens(ApiUser user, DiscordUpdateResponse dto) {
		user.DiscordAccessToken = dto.AccessToken;
		user.DiscordAccessTokenExpires = dto.AccessTokenExpires;
		user.DiscordRefreshToken = dto.RefreshToken;
		user.DiscordAccessTokenExpires = dto.RefreshTokenExpires;
	}
	
	public async Task TriggerAuthTokenRefresh(string userId) {
		var data = new JobDataMap { { "AccountId", userId } };
		var scheduler = await schedulerFactory.GetScheduler();
		await scheduler.TriggerJob(RefreshAuthTokenBackgroundTask.Key, data);
	}
}