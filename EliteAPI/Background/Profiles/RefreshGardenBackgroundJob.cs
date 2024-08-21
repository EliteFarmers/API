using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Profiles;

public class RefreshGardenBackgroundJob(
    IMessageService messageService,
    ILogger<RefreshGardenBackgroundJob> logger,
    IConnectionMultiplexer redis,
    IHypixelService hypixelService,
    IOptions<ConfigCooldownSettings> coolDowns,
    ILeaderboardService leaderboardService,
    DataContext context)
    : IJob {
    public static readonly JobKey Key = new(nameof(RefreshGardenBackgroundJob));
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

    public async Task Execute(IJobExecutionContext executionContext) {
        var profileId = executionContext.MergedJobDataMap.GetString("ProfileId");
        if (string.IsNullOrWhiteSpace(profileId)) return;

        if (executionContext.RefireCount > 1) {
            messageService.SendErrorMessage("Refresh Garden Background Job",
                "Failed to refresh garden. Refire count exceeded.\n" +
                $"ProfileId: {profileId}");
            return;
        }

        try {
            await RefreshGarden(profileId);
        }
        catch (Exception e) {
            messageService.SendErrorMessage("Failed Refresh Garden", $"ProfileId: {profileId}\n" + e.Message);
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
    }

    private async Task RefreshGarden(string profileId) {
        var key = $"refresh:garden:{profileId}";
        var db = redis.GetDatabase();
        if (db.KeyExists(key)) {
            logger.LogDebug("Garden is still on cooldown");
            return;
        }

        await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(_coolDowns.SkyblockGardenCooldown));

        var garden = await context.Gardens
            .FirstOrDefaultAsync(g => g.ProfileId == profileId);
        
        if (garden is not null && !garden.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockGardenCooldown)) return;
        
        // Fetch new data
        var incoming = await hypixelService.FetchGarden(profileId);
        if (incoming.Value?.Success is not true || incoming.Value.Garden is null) {
            // Add empty garden to prevent constant fetch attempts
            
            if (garden is null) {
                context.Gardens.Add(new Garden {
                    ProfileId = profileId,
                    LastUpdated = DateTimeOffset.UtcNow,
                });
            } else {
                garden.LastUpdated = DateTimeOffset.UtcNow;
                context.Gardens.Update(garden);
            }
            
            await context.SaveChangesAsync();
            return;
        }
        
        var gardenData = incoming.Value.Garden;
        var plots = gardenData.CombinePlots();
        var visitors = gardenData.Visitors?.CombineVisitors() ?? new Dictionary<string, VisitorData>();
        
        if (garden is null) {
            var newGarden = new Garden {
                ProfileId = profileId,

                GardenExperience = (long)gardenData.GardenExperience,
                UnlockedPlots = (UnlockedPlots)plots,

                CompletedVisitors = gardenData.Visitors?.TotalVisitorsServed ?? 0,
                UniqueVisitors = gardenData.Visitors?.UniqueVisitorsServed ?? 0,
                
                Visitors = visitors,
                Composter = gardenData.Composter,
            };
            
            newGarden.PopulateCropMilestones(gardenData);
            newGarden.PopulateCropUpgrades(gardenData);
            
            context.Gardens.Add(newGarden);
        } else {
            garden.LastUpdated = DateTimeOffset.UtcNow;
            garden.GardenExperience = (long)gardenData.GardenExperience;
            garden.UnlockedPlots = (UnlockedPlots)plots;
            
            garden.CompletedVisitors = gardenData.Visitors?.TotalVisitorsServed ?? 0;
            garden.UniqueVisitors = gardenData.Visitors?.UniqueVisitorsServed ?? 0;
            
            garden.Visitors = visitors;
            garden.Composter = gardenData.Composter;
            
            garden.PopulateCropMilestones(gardenData);
            garden.PopulateCropUpgrades(gardenData);
            
            context.Gardens.Update(garden);
            
            UpdateLeaderboardPlacements(garden);
        }
        
        await context.SaveChangesAsync();
    }

    private void UpdateLeaderboardPlacements(Garden garden) {
        var id = garden.ProfileId;
        // General garden placements
        leaderboardService.UpdateLeaderboardScore("garden", id, garden.GardenExperience);
        leaderboardService.UpdateLeaderboardScore("visitors-accepted", id, garden.CompletedVisitors);
        // Crop milestones
        leaderboardService.UpdateLeaderboardScore("cactus-milestone", id, garden.Crops.Cactus);
        leaderboardService.UpdateLeaderboardScore("carrot-milestone", id, garden.Crops.Carrot);
        leaderboardService.UpdateLeaderboardScore("cocoa-milestone", id, garden.Crops.CocoaBeans);
        leaderboardService.UpdateLeaderboardScore("melon-milestone", id, garden.Crops.Melon);
        leaderboardService.UpdateLeaderboardScore("mushroom-milestone", id, garden.Crops.Mushroom);
        leaderboardService.UpdateLeaderboardScore("netherwart-milestone", id, garden.Crops.NetherWart);
        leaderboardService.UpdateLeaderboardScore("potato-milestone", id, garden.Crops.Potato);
        leaderboardService.UpdateLeaderboardScore("pumpkin-milestone", id, garden.Crops.Pumpkin);
        leaderboardService.UpdateLeaderboardScore("sugarcane-milestone", id, garden.Crops.SugarCane);
        leaderboardService.UpdateLeaderboardScore("wheat-milestone", id, garden.Crops.Wheat);
    }
}