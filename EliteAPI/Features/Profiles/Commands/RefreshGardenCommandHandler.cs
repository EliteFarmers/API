using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Parsers.Farming;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Features.Profiles.Commands;

public class RefreshGardenCommandHandler(
	IServiceScopeFactory scopeFactory,
	IConnectionMultiplexer redis,
	IMessageService messageService
) : ICommandHandler<RefreshGardenCommand>
{
	public async Task ExecuteAsync(RefreshGardenCommand command, CancellationToken ct) {
		var lockKey = $"refresh:garden:{command.ProfileId}";
		var db = redis.GetDatabase();

		if (!await db.LockTakeAsync(lockKey, "1", TimeSpan.FromMinutes(2))) {
			return;
		}

		try {
			using var scope = scopeFactory.CreateScope();
			
			var context = scope.ServiceProvider.GetRequiredService<DataContext>();
			var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();
			var coolDowns = scope.ServiceProvider.GetRequiredService<IOptions<ConfigCooldownSettings>>().Value;

			var garden = await context.Gardens
				.FirstOrDefaultAsync(g => g.ProfileId == command.ProfileId, ct);

			if (garden is not null && !garden.LastUpdated.OlderThanSeconds(coolDowns.SkyblockGardenCooldown)) {
				return;
			}

			var incoming = await hypixelService.FetchGarden(command.ProfileId, ct);
			if (incoming.Value?.Success is not true || incoming.Value.Garden is null) {
				if (garden is null) {
					context.Gardens.Add(new EliteAPI.Models.Entities.Hypixel.Garden {
						ProfileId = command.ProfileId,
						LastUpdated = DateTimeOffset.UtcNow,
						ProfileResponseHash = command.ProfileResponseHash
					});
				} else {
					garden.LastUpdated = DateTimeOffset.UtcNow;
					garden.ProfileResponseHash = command.ProfileResponseHash;
					context.Gardens.Update(garden);
				}

				await context.SaveChangesAsync(ct);
				return;
			}

			var gardenData = incoming.Value.Garden;
			var plots = gardenData.CombinePlots();
			var visitors = gardenData.CombineVisitors();
			var (greenhouseSlotsMaskLow, greenhouseSlotsMaskHigh) = GreenhouseSlotParser.EncodeSlots(gardenData.GreenhouseSlots);
			var gardenUpgrades = new EliteAPI.Models.Entities.Hypixel.GardenUpgradesData {
				GreenhouseYield = gardenData.GardenUpgrades.GreenhouseYield,
				GreenhousePlotLimit = gardenData.GardenUpgrades.GreenhousePlotLimit,
				GreenhouseGrowthSpeed = gardenData.GardenUpgrades.GreenhouseGrowthSpeed
			};

			if (garden is null) {
				var newGarden = new EliteAPI.Models.Entities.Hypixel.Garden {
					ProfileId = command.ProfileId,

					GardenExperience = (long)gardenData.GardenExperience,
					UnlockedPlots = (EliteAPI.Models.Entities.Hypixel.UnlockedPlots)plots,

					CompletedVisitors = gardenData.Visitors?.TotalVisitorsServed ?? 0,
					UniqueVisitors = gardenData.Visitors?.UniqueVisitorsServed ?? 0,

					LastGrowthStageTime = gardenData.LastGrowthStageTime,
					GreenhouseSlotsMaskLow = greenhouseSlotsMaskLow,
					GreenhouseSlotsMaskHigh = greenhouseSlotsMaskHigh,
					Visitors = visitors,
					Composter = gardenData.Composter,
					GardenUpgrades = gardenUpgrades,
					ProfileResponseHash = command.ProfileResponseHash
				};

				newGarden.PopulateCropMilestones(gardenData);
				newGarden.PopulateCropUpgrades(gardenData);

				context.Gardens.Add(newGarden);
			} else {
				garden.LastUpdated = DateTimeOffset.UtcNow;
				garden.GardenExperience = (long)gardenData.GardenExperience;
				garden.UnlockedPlots = (EliteAPI.Models.Entities.Hypixel.UnlockedPlots)plots;

				garden.CompletedVisitors = gardenData.Visitors?.TotalVisitorsServed ?? 0;
				garden.UniqueVisitors = gardenData.Visitors?.UniqueVisitorsServed ?? 0;

				garden.LastGrowthStageTime = gardenData.LastGrowthStageTime;
				garden.GreenhouseSlotsMaskLow = greenhouseSlotsMaskLow;
				garden.GreenhouseSlotsMaskHigh = greenhouseSlotsMaskHigh;
				garden.Visitors = visitors;
				garden.Composter = gardenData.Composter;
				garden.GardenUpgrades = gardenUpgrades;
				garden.ProfileResponseHash = command.ProfileResponseHash;

				garden.PopulateCropMilestones(gardenData);
				garden.PopulateCropUpgrades(gardenData);

				context.Gardens.Update(garden);
			}

			await context.SaveChangesAsync(ct);
		}
		catch (Exception e) {
			messageService.SendErrorMessage(
				"Refresh Garden Command",
				$"Failed to refresh garden.\n" +
				$"ProfileId: {command.ProfileId}\n" +
				$"Error: {e.Message}");
			throw;
		}
		finally {
			await db.LockReleaseAsync(lockKey, "1");
		}
	}
}
