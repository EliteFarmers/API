using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using EliteAPI.Background.Discord;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Auth;
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
	ILogger<AuthService> logger,
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
			logger.LogWarning("Failed to fetch refresh token for user login with access code!");
			return null;
		}

		var account = await discordService.GetDiscordUser(login.AccessToken);

		if (account is null) {
			logger.LogWarning("Failed to fetch Discord user account for login!");
			return null;
		}

		// Register the user if they do not exist
		var user = await userManager.FindByIdAsync(account.Id.ToString());
		if (user is null) {
			var errors = await RegisterUser(account, login);
			var identityErrors = errors.ToList();
			if (identityErrors.Count != 0) {
				logger.LogWarning("Failed to register user {UserId} with errors: {Errors}", account.Id,
					string.Join(", ", identityErrors.Select(e => e.Description)));
				return null;
			}

			user = await userManager.FindByIdAsync(account.Id.ToString());
			if (user is null) {
				logger.LogError("User {UserId} was not found after registration!", account.Id);
				return null; // Should not happen if registration succeeded
			}
		}
		else {
			// Update existing user if necessary
			if (user.UserName != account.Username) {
				await ObtainUserName(account.Username);
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
			logger.LogWarning("Failed to generate or store refresh token for user {UserId}", user.Id);
			return null;
		}

		return new AuthResponseDto {
			AccessToken = token,
			ExpiresIn = expiry.ToUnixTimeSeconds().ToString(),
			RefreshToken = refreshToken
		};
	}

	private async Task<string?> GenerateAndStoreRefreshToken(ApiUser user) {
		// Check if there's a token that was created in the last minute
		var exisingToken = await context.RefreshTokens
			.FirstOrDefaultAsync(rt =>
				rt.UserId == user.Id && rt.RevokedUtc == null && rt.CreatedUtc > DateTime.UtcNow.AddMinutes(-1));

		if (exisingToken is not null) return exisingToken.Token; // Return existing token if found

		var randomNumber = new byte[64]; // Generate a secure random token
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(randomNumber);
		var refreshTokenValue = Convert.ToBase64String(randomNumber);

		var refreshTokenValidityInDays = configuration.GetValue("Jwt:RefreshTokenExpirationInDays", 30);

		var refreshToken = new RefreshToken {
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
		if (DiscordIdRegex().IsMatch(dto.UserId)) return await VerifyRefreshToken(dto.UserId, dto.RefreshToken);

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

		if (refreshToken.Contains('%')) refreshToken = HttpUtility.UrlDecode(refreshToken);

		var storedToken = await context.RefreshTokens
			.FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);

		if (storedToken is null || !storedToken.IsActive) return null;

		// Load user settings relationship
		await context.Entry(user).Reference(x => x.Account).LoadAsync();
		await context.Entry(user.Account).Collection(x => x.MinecraftAccounts).LoadAsync();
		await context.Entry(user.Account).Reference(x => x.UserSettings).LoadAsync();

		var newRefreshTokenValue = await GenerateAndStoreRefreshToken(user);
		if (newRefreshTokenValue.IsNullOrEmpty()) return null;

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
		await context.Entry(user.Account).Collection(x => x.MinecraftAccounts).LoadAsync();

		var primaryAccount = user.Account.MinecraftAccounts.FirstOrDefault(q => q.Selected)
		                     ?? user.Account.MinecraftAccounts.FirstOrDefault();

		var claims = new List<Claim> {
			new(ClaimNames.Name, user.UserName ?? string.Empty),
			new(ClaimNames.NameId, user.Id),
			new(ClaimNames.Jti, Guid.NewGuid().ToString()),
			new(ClaimNames.Avatar, user.Account.Avatar ?? string.Empty),
			new(ClaimNames.Ign, primaryAccount?.Name ?? string.Empty),
			new(ClaimNames.FormattedIgn, user.Account.GetFormattedIgn()),
			new(ClaimNames.Uuid, primaryAccount?.Id ?? string.Empty)
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

	/// <summary>
	/// Usernames are unique per Discord account, but a user might not have logged in since changing their username,
	/// which would allow a different user to register with the same username.
	/// <br/>
	/// To prevent this, we check if the username already exists in the database, and if it does, we update the existing user
	/// to a random GUID as a placeholder username until they log in again. (usernames are not used for anything important)
	/// </summary>
	/// <returns></returns>
	private async Task ObtainUserName(string username) {
		var existingUser = await userManager.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.UserName == username);

		if (existingUser is null) return;

		var placeholderUsername = Guid.NewGuid().ToString();
		existingUser.UserName = placeholderUsername;
		var updateResult = await userManager.UpdateAsync(existingUser);

		if (!updateResult.Succeeded) {
			logger.LogWarning("Failed to update existing user {UserId} with placeholder username: {Errors}",
				existingUser.Id,
				string.Join(", ", updateResult.Errors.Select(e => e.Description)));
			return;
		}

		logger.LogInformation("Updated existing user {UserId} username to placeholder: {Placeholder}",
			existingUser.Id,
			placeholderUsername);
	}

	private async Task<IEnumerable<IdentityError>> RegisterUser(EliteAccount account, DiscordUpdateResponse dto) {
		await ObtainUserName(account.Username);

		var user = new ApiUser {
			Id = account.Id.ToString(),
			AccountId = account.Id,
			UserName = account.Username
		};

		UpdateUserDiscordTokens(user, dto);

		var result = await userManager.CreateAsync(user);

		if (result.Succeeded) await userManager.AddToRoleAsync(user, "User");

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