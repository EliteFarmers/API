using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Discord;

public class RefreshAuthTokenBackgroundTask(
	UserManager<ApiUser> userManager,
	IConnectionMultiplexer redis,
	IDiscordService discordService,
	ILogger<RefreshAuthTokenBackgroundTask> logger,
	IMessageService messageService) 
	: IJob 
{
	public static readonly JobKey Key = new(nameof(RefreshAuthTokenBackgroundTask));
	
	public async Task Execute(IJobExecutionContext executionContext) {
		var accountId = executionContext.MergedJobDataMap.GetString("AccountId");
		if (string.IsNullOrWhiteSpace(accountId)) return;
		
		var user = await userManager.FindByIdAsync(accountId);
		if (user is null) return;
		
		if (executionContext.RefireCount > 1) {
			messageService.SendErrorMessage("Refresh Auth Token Failed", 
				"Failed to refresh auth token. Refire count exceeded.\n" +
				$"AccountId: `{accountId}`");
			return;
		}

		var key = $"discord:refresh:{user.Id}";
		var lockToken = Guid.NewGuid().ToString();
		var db = redis.GetDatabase();
		
		// Ensure only one instance of this job is running at a time
		if (await db.LockTakeAsync(key, lockToken, TimeSpan.FromMinutes(1))) {
			try {
				logger.LogInformation("Refreshing auth token for user {UserId}", user.Id);
				await RefreshAuthToken(user);
			}  catch (Exception e) {
				messageService.SendErrorMessage("Failed Process Jacob Contests", e.Message);
				throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
			} finally {
				await db.LockReleaseAsync(key, lockToken);
			}
		}
	}
	
	public async Task RefreshAuthToken(ApiUser user) {
		var refreshToken = user.DiscordRefreshToken;
		
		if (string.IsNullOrWhiteSpace(refreshToken) || user.DiscordRefreshTokenExpires < DateTime.UtcNow) {
			return;
		}
		
		var newTokens = await discordService.RefreshDiscordUser(refreshToken);
		
		if (newTokens is null) {
			logger.LogWarning("Failed to refresh auth token for user {UserId}", user.Id);
			
			user.DiscordRefreshToken = null;
			user.DiscordAccessToken = null;
			await userManager.UpdateAsync(user);
			
			return;
		}
		
		user.DiscordAccessToken = newTokens.AccessToken;
		user.DiscordRefreshToken = newTokens.RefreshToken;
		user.DiscordRefreshTokenExpires = newTokens.RefreshTokenExpires;
		user.DiscordAccessTokenExpires = newTokens.AccessTokenExpires;

		await userManager.UpdateAsync(user);
	}
}