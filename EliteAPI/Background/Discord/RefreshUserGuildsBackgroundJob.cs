using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Quartz;

namespace EliteAPI.Background.Discord;

public class RefreshUserGuildsBackgroundJob(
    ILogger<RefreshUserGuildsBackgroundJob> logger,
    UserManager<ApiUser> userManager,
    IDiscordService discordService,
    IMessageService messageService
	) : IJob
{
    public static readonly JobKey Key = new(nameof(RefreshUserGuildsBackgroundJob));

	public async Task Execute(IJobExecutionContext executionContext) {
        var userId = executionContext.MergedJobDataMap.GetString("userId");
        var token = executionContext.MergedJobDataMap.GetString("token");
        
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token)) {
            messageService.SendErrorMessage("Failed to fetch Discord Guilds", "Missing userId or token");
            return;
        }
        
        logger.LogInformation("Fetching user guilds from Discord - {UtcNow}", DateTime.UtcNow);

        if (executionContext.RefireCount > 3) {
            messageService.SendErrorMessage("Failed to fetch user Discord Guilds", "Failed to fetch user guilds from Discord");
            return;
        }

        try {
            await RefreshUserGuilds(userId, token);
        } catch (Exception e) {
            messageService.SendErrorMessage("Failed to fetch Discord Guilds", e.Message);
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
    }
	
	private async Task RefreshUserGuilds(string userId, string token) {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return;
        
        await discordService.FetchUserGuilds(user, token);
    }
}