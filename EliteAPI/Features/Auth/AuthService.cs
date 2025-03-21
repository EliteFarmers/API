﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EliteAPI.Background.Discord;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using Quartz;

namespace EliteAPI.Features.Auth;

[RegisterService<IAuthService>(LifeTime.Scoped)]
public class AuthService(
	IDiscordService discordService, 
	UserManager userManager,
	IConfiguration configuration,
	ISchedulerFactory schedulerFactory,
	DataContext context) 
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

	public async Task<AuthResponseDto?> VerifyRefreshToken(AuthRefreshDto dto) {
		var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
		var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(dto.AccessToken);
		
		var userId = tokenContent.Claims.ToList()
			.FirstOrDefault(q => q.Type == ClaimTypes.NameIdentifier)?.Value;
		if (userId is null) return null;

		return await VerifyRefreshToken(userId, dto.RefreshToken);
	}

	public async Task<AuthResponseDto?> VerifyRefreshToken(string userId, string refreshToken) {
		var user = await userManager.FindByIdAsync(userId);
		if (user is null) return null;
		
		var isValid = await userManager.VerifyUserTokenAsync(user, LoginProvider, RefreshToken, refreshToken);

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
				user.DiscordAccessTokenExpires = response.AccessTokenExpires;
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
		// Load user accounts (and minecraft accounts)
		await context.Entry(user).Reference(x => x.Account).LoadAsync();
		
		var primaryAccount = user.Account.MinecraftAccounts.FirstOrDefault(q => q.Selected)
			?? user.Account.MinecraftAccounts.FirstOrDefault();
		
		var claims = new List<Claim> {
			new(ClaimTypes.Name, user.UserName ?? string.Empty),
			new(ClaimTypes.NameIdentifier, user.Id),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new(ApiUserClaims.Avatar, user.Account.Avatar ?? string.Empty),
			new(ApiUserClaims.Ign, primaryAccount?.Name ?? string.Empty),
			new(ApiUserClaims.Uuid, primaryAccount?.Id ?? string.Empty),
			new(ApiUserClaims.DiscordAccessExpires, user.DiscordAccessTokenExpires.ToUnixTimeSeconds().ToString())
		};
		
		// Add roles to the claims
		var roles = await userManager.GetRolesAsync(user);
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		var expiresAt = configuration["Jwt:TokenExpirationInMinutes"] is { } expiration
			? DateTime.UtcNow.AddMinutes(int.Parse(expiration))
			: DateTime.UtcNow.AddMinutes(30);
		
		var token = JwtBearer.CreateToken(o => {
			o.User.Claims.AddRange(claims);
			o.ExpireAt = expiresAt;
		});

		return (token, expiresAt);
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
			? DateTimeOffset.UtcNow.AddMilliseconds(expiresIn - 5000) // Subtract 5 seconds to add wiggle room for refreshing
			: DateTimeOffset.UtcNow.AddMinutes(8);

		user.DiscordAccessToken = dto.AccessToken;
		user.DiscordAccessTokenExpires = accessExpires;
		user.DiscordRefreshToken = dto.RefreshToken;
		user.DiscordAccessTokenExpires = accessExpires.AddDays(20);
	}
	
	public async Task TriggerAuthTokenRefresh(string userId) {
		var data = new JobDataMap { { "AccountId", userId } };
		var scheduler = await schedulerFactory.GetScheduler();
		await scheduler.TriggerJob(RefreshAuthTokenBackgroundTask.Key, data);
	}
}