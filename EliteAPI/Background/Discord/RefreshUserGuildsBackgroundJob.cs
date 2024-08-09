using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Discord;

public class RefreshUserGuildsBackgroundJob(
    ILogger<RefreshUserGuildsBackgroundJob> logger,
    UserManager<ApiUser> userManager,
    IDiscordService discordService,
    IConnectionMultiplexer redis,
    IMessageService messageService
	) : IJob
{
    public static readonly JobKey Key = new(nameof(RefreshUserGuildsBackgroundJob));

	public async Task Execute(IJobExecutionContext executionContext) {
        var userId = executionContext.MergedJobDataMap.GetString("userId");
        
        if (string.IsNullOrWhiteSpace(userId)) {
            messageService.SendErrorMessage("Failed to fetch Discord Guilds", "Missing userId");
            return;
        }
        
        logger.LogInformation("Fetching user guilds from Discord - {UtcNow}", DateTime.UtcNow);

        if (executionContext.RefireCount > 3) {
            messageService.SendErrorMessage("Failed to fetch user Discord Guilds", "Failed to fetch user guilds from Discord");
            return;
        }
        
        var key = $"discord:guilds_refresh:{userId}";
        var db = redis.GetDatabase();
		
        // Ensure only one instance of this job is running at a time
        if (await db.LockTakeAsync(key, "1", TimeSpan.FromMinutes(1))) {
            try {
                await RefreshUserGuilds(userId);
            }  catch (Exception e) {
                messageService.SendErrorMessage("Failed to fetch Discord Guilds", e.Message);
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
            } finally {
                await db.LockReleaseAsync(key, "1");
            }
        }
    }
	
	private async Task RefreshUserGuilds(string userId) {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return;
        
        await discordService.FetchUserGuilds(user);
    }
}