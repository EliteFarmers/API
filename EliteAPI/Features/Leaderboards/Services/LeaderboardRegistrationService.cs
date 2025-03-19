using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Definitions;
using EliteAPI.Features.Leaderboards.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILeaderboardRegistrationService {
	public List<ILeaderboardDefinition> Leaderboards { get; }
	Task RegisterLeaderboardsAsync(CancellationToken c);
}

[RegisterService<ILeaderboardRegistrationService>(LifeTime.Singleton)]
public class LeaderboardRegistrationService(IServiceScopeFactory provider) : ILeaderboardRegistrationService
{
	public List<ILeaderboardDefinition> Leaderboards { get; } = [
		new FarmingWeightLeaderboard(),
		new GardenXpLeaderboard(),
		new JacobContestsLeaderboard(),
		new JacobFirstPlaceContestsLeaderboard(),
		new JacobMedalsBronzeLeaderboard(),
		new JacobMedalsSilverLeaderboard(),
		new JacobMedalsGoldLeaderboard(),
		new JacobMedalsPlatinumLeaderboard(),
		new JacobMedalsDiamondLeaderboard(),
		new SkyblockLevelLeaderboard(),
		new TotalChocolateLeaderboard(),
		new VisitorsAcceptedLeaderboard(),
		new CropCactusLeaderboard(),
		new CropCarrotLeaderboard(),
		new CropCocoaLeaderboard(),
		new CropMelonLeaderboard(),
		new CropMushroomLeaderboard(),
		new CropNetherWartLeaderboard(),
		new CropPotatoLeaderboard(),
		new CropPumpkinLeaderboard(),
		new CropSugarCaneLeaderboard(),
		new CropWheatLeaderboard(),
		new PestMiteLeaderboard(),
		new PestCricketLeaderboard(),
		new PestMothLeaderboard(),
		new PestEarthwormLeaderboard(),
		new PestSlugLeaderboard(),
		new PestBeetleLeaderboard(),
		new PestLocustLeaderboard(),
		new PestRatLeaderboard(),
		new PestMosquitoLeaderboard(),
		new PestFlyLeaderboard(),
		new PestFieldMouseLeaderboard()
	];

	public async Task RegisterLeaderboardsAsync(CancellationToken c) {
		using var scope = provider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<DataContext>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaderboardRegistrationService>>();
		
		var start = DateTime.UtcNow;
		foreach (var leaderboard in Leaderboards) {
			await UpdateLeaderboard(leaderboard);
		}

		await context.SaveChangesAsync(c);
		logger.LogInformation("Registration of {Sum} leaderboards took {UtcNow}ms", Leaderboards.Select(l => l.Info.IntervalType.Count).Sum(), DateTime.UtcNow - start);
		return;

		async Task UpdateLeaderboard(ILeaderboardDefinition leaderboard) {
			foreach (var intervalType in leaderboard.Info.IntervalType) {
				switch (intervalType) {
					case LeaderboardType.Current:
						await CreateLeaderboardIfNotExists(leaderboard, intervalType, $"{leaderboard.Info.Slug}");
						break;
					case LeaderboardType.Monthly:
						await CreateLeaderboardIfNotExists(leaderboard, intervalType, $"{leaderboard.Info.Slug}-monthly");
						break;
					case LeaderboardType.Weekly:
						await CreateLeaderboardIfNotExists(leaderboard, intervalType, $"{leaderboard.Info.Slug}-weekly");
						break;
				}
			}
		}

		async Task CreateLeaderboardIfNotExists(ILeaderboardDefinition leaderboard, LeaderboardType type, string slug) {
			var existing = await context.Leaderboards
				.FirstOrDefaultAsync(lb => lb.Slug.Equals(slug), cancellationToken: c);

			if (existing is not null) {
				existing.ShortTitle = leaderboard.Info.ShortTitle;
				existing.Title = leaderboard.Info.Title;
				existing.IntervalType = type;
				existing.ScoreDataType = leaderboard.Info.ScoreDataType;
				return;
			}

			var newLeaderboard = new Leaderboard() {
				Title = leaderboard.Info.Title,
				ShortTitle = leaderboard.Info.ShortTitle,
				Slug = slug,
				IntervalType = type,
				ScoreDataType = leaderboard.Info.ScoreDataType
			};

			await context.Leaderboards.AddAsync(newLeaderboard, c);
		}
	}
}