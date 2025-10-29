using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Definitions;
using EliteAPI.Features.Leaderboards.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILeaderboardRegistrationService
{
	public List<ILeaderboardDefinition> Leaderboards { get; }
	public Dictionary<string, ILeaderboardDefinition> LeaderboardsById { get; }
	Task RegisterLeaderboardsAsync(CancellationToken c);
}

[RegisterService<ILeaderboardRegistrationService>(LifeTime.Singleton)]
public class LeaderboardRegistrationService(IServiceScopeFactory provider) : ILeaderboardRegistrationService
{
	public async Task RegisterLeaderboardsAsync(CancellationToken c) {
		using var scope = provider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<DataContext>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaderboardRegistrationService>>();

		var start = DateTime.UtcNow;
		foreach (var leaderboard in Leaderboards) {
			await UpdateLeaderboard(leaderboard);
		}

		await context.SaveChangesAsync(c);
		logger.LogInformation("Registration of {Sum} leaderboards took {UtcNow}ms",
			Leaderboards.Select(l => l.Info.IntervalType.Count).Sum(), DateTime.UtcNow - start);
		return;

		async Task UpdateLeaderboard(ILeaderboardDefinition leaderboard) {
			foreach (var intervalType in leaderboard.Info.IntervalType) {
				switch (intervalType) {
					case LeaderboardType.Current:
						await CreateLeaderboardIfNotExists(leaderboard, intervalType, $"{leaderboard.Info.Slug}");
						break;
					case LeaderboardType.Monthly:
						await CreateLeaderboardIfNotExists(leaderboard, intervalType,
							$"{leaderboard.Info.Slug}-monthly");
						break;
					case LeaderboardType.Weekly:
						await CreateLeaderboardIfNotExists(leaderboard, intervalType,
							$"{leaderboard.Info.Slug}-weekly");
						break;
				}
			}
		}

		async Task CreateLeaderboardIfNotExists(ILeaderboardDefinition leaderboard, LeaderboardType type, string slug) {
			var existing = await context.Leaderboards
				.FirstOrDefaultAsync(lb => lb.Slug.Equals(slug), c);

			if (!LeaderboardsById.TryAdd(slug, leaderboard))
				throw new InvalidOperationException($"Leaderboard with slug {slug} is already registered");

			if (existing is not null) {
				existing.ShortTitle = leaderboard.Info.ShortTitle;
				existing.Title = leaderboard.Info.Title;
				existing.IntervalType = type;
				existing.ScoreDataType = leaderboard.Info.ScoreDataType;

				if (leaderboard.Info.MinimumScore > existing.MinimumScore) {
					var count = 0;
					if (type == LeaderboardType.Current)
						count = await context.LeaderboardEntries
							.Where(s =>
								s.LeaderboardId == existing.LeaderboardId
								&& s.IntervalIdentifier == null
								&& s.Score < leaderboard.Info.MinimumScore)
							.ExecuteDeleteAsync(c);
					else
						count += await context.LeaderboardEntries
							.Where(s =>
								s.LeaderboardId == existing.LeaderboardId
								&& s.IntervalIdentifier != null
								&& s.InitialScore < leaderboard.Info.MinimumScore)
							.ExecuteDeleteAsync(c);

					logger.LogInformation(
						"Deleted {Count} entries from \"{Slug}\" leaderboard that had less than the minimum score",
						count, slug);
				}

				existing.MinimumScore = leaderboard.Info.MinimumScore;
				return;
			}

			var newLeaderboard = new Leaderboard {
				Title = leaderboard.Info.Title,
				ShortTitle = leaderboard.Info.ShortTitle,
				Slug = slug,
				IntervalType = type,
				ScoreDataType = leaderboard.Info.ScoreDataType,
				MinimumScore = leaderboard.Info.MinimumScore
			};

			await context.Leaderboards.AddAsync(newLeaderboard, c);
		}
	}

	public Dictionary<string, ILeaderboardDefinition> LeaderboardsById { get; } = new();

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
		new MilestoneCactusLeaderboard(),
		new MilestoneCarrotLeaderboard(),
		new MilestoneCocoaLeaderboard(),
		new MilestoneMelonLeaderboard(),
		new MilestoneMushroomLeaderboard(),
		new MilestoneNetherWartLeaderboard(),
		new MilestonePotatoLeaderboard(),
		new MilestonePumpkinLeaderboard(),
		new MilestoneSugarCaneLeaderboard(),
		new MilestoneWheatLeaderboard(),
		new TotalPestKillsLeaderboard(),
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
		new PestFieldMouseLeaderboard(),
		new SkillAlchemyLeaderboard(),
		new SkillCarpentryLeaderboard(),
		new SkillCombatLeaderboard(),
		new SkillEnchantingLeaderboard(),
		new SkillFarmingLeaderboard(),
		new SkillFishingLeaderboard(),
		new SkillForagingLeaderboard(),
		new SkillMiningLeaderboard(),
		new SkillRunecraftingLeaderboard(),
		new SkillSocialLeaderboard(),
		new SkillTamingLeaderboard(),
		new SkillSocialProfileLeaderboard(),
		new CoalLeaderboard(),
		new CobblestoneLeaderboard(),
		new DiamondLeaderboard(),
		new EmeraldLeaderboard(),
		new EndStoneLeaderboard(),
		new GemstoneLeaderboard(),
		new GlaciteLeaderboard(),
		new GlowstoneLeaderboard(),
		new GoldLeaderboard(),
		new GravelLeaderboard(),
		new HardStoneLeaderboard(),
		new IceLeaderboard(),
		new IronLeaderboard(),
		new LapisLeaderboard(),
		new MithrilLeaderboard(),
		new MyceliumLeaderboard(),
		new NetherQuartzLeaderboard(),
		new NetherrackLeaderboard(),
		new ObsidianLeaderboard(),
		new RedSandLeaderboard(),
		new RedstoneLeaderboard(),
		new SandLeaderboard(),
		new SulphurLeaderboard(),
		new TungstenLeaderboard(),
		new UmberLeaderboard(),
		new SeedsLeaderboard(),
		new RawChickenLeaderboard(),
		new RawRabbitLeaderboard(),
		new MuttonLeaderboard(),
		new LeatherLeaderboard(),
		new FeatherLeaderboard(),
		new RawPorkchopLeaderboard(),
		new BlazeRodLeaderboard(),
		new BoneLeaderboard(),
		new ChiliPepperLeaderboard(),
		new EnderPearlLeaderboard(),
		new GhastTearLeaderboard(),
		new GunpowderLeaderboard(),
		new MagmaCreamLeaderboard(),
		new RottenFleshLeaderboard(),
		new SlimeballLeaderboard(),
		new SpiderEyeLeaderboard(),
		new StringLeaderboard(),
		new AcaciaLeaderboard(),
		new BirchLeaderboard(),
		new DarkOakLeaderboard(),
		new JungleLeaderboard(),
		new OakLeaderboard(),
		new SpruceLeaderboard(),
		new MangroveLeaderboard(),
		new FigLeaderboard(),
		new SeaLumiesLeaderboard(),
		new VinesapLeaderboard(),
		new LushlilacLeaderboard(),
		new TenderWoodLeaderboard(),
		new ClayLeaderboard(),
		new ClownfishLeaderboard(),
		new InkSacLeaderboard(),
		new LilyPadLeaderboard(),
		new MagmafishLeaderboard(),
		new PrismarineCrystalsLeaderboard(),
		new PrismarineShardLeaderboard(),
		new PufferfishLeaderboard(),
		new RawFishLeaderboard(),
		new RawSalmonLeaderboard(),
		new SpongeLeaderboard(),
		new AgaricusCapLeaderboard(),
		new CaducousStemLeaderboard(),
		new HalfEatenCarrotLeaderboard(),
		new HemovibeLeaderboard(),
		new LivingMetalHeartLeaderboard(),
		new WiltedBerberisLeaderboard(),
		new TimiteLeaderboard(),
		new CatacombsLeaderboard(),
		new ArcherLeaderboard(),
		new BerserkLeaderboard(),
		new HealerLeaderboard(),
		new MageLeaderboard(),
		new TankLeaderboard(),
	];
}