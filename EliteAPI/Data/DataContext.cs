using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Announcements.Models;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Confirmations.Models;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Features.Images.Models;
using EliteAPI.Features.Resources.Bazaar;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Features.Recap.Models;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Firesales.Models;
using EliteAPI.Features.Resources.Items.Models;
using EliteAPI.Features.Textures.Models;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Models.Entities.Timescale;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EliteAPI.Data;

public class DataContext(DbContextOptions<DataContext> options, IConfiguration config)
	: IdentityDbContext<ApiUser>(options)
{
	private static NpgsqlDataSource? Source { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder);

		// Get connection string from config "PostgresConnection"
		var connection = config.GetConnectionString("Postgres");

		if (string.IsNullOrEmpty(connection)) {
			Console.WriteLine("No connection string found. Quitting...");
			Environment.Exit(1);
			return;
		}

		if (Source is null) {
			var builder = new NpgsqlDataSourceBuilder(connection);
			builder.EnableDynamicJson();
			Source = builder.Build();
		}

		optionsBuilder.EnableSensitiveDataLogging(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ==
		                                          "Development");
		optionsBuilder.UseNpgsql(Source, opt => { opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); });
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		// This automatically applies all IEntityTypeConfiguration implementations in the assembly
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);

		modelBuilder.HasCollation("case_insensitive", "en-u-ks-primary", "icu", false);

		modelBuilder.Entity<MinecraftAccount>().Property(c => c.Name)
			.UseCollation("case_insensitive");

		modelBuilder.Entity<MinecraftAccount>().Navigation(e => e.Badges).AutoInclude();
		modelBuilder.Entity<UserBadge>().Navigation(e => e.Badge).AutoInclude();

		modelBuilder.Entity<Entitlement>().HasDiscriminator(e => e.Target)
			.HasValue<Entitlement>(EntitlementTarget.None)
			.HasValue<UserEntitlement>(EntitlementTarget.User)
			.HasValue<GuildEntitlement>(EntitlementTarget.Guild);

		// Guides configuration is handled via IEntityTypeConfiguration in model files
	}

	// Auth
	public DbSet<EliteAccount> Accounts { get; set; } = null!;
	public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

	public DbSet<MinecraftAccount> MinecraftAccounts { get; set; } = null!;
	public DbSet<UserSettings> UserSettings { get; set; } = null!;
	public DbSet<Badge> Badges { get; set; } = null!;
	public DbSet<UserBadge> UserBadges { get; set; } = null!;
	public DbSet<Profile> Profiles { get; set; } = null!;
	public DbSet<GameModeHistory> GameModeHistories { get; set; } = null!;
	public DbSet<ProfileMember> ProfileMembers { get; set; } = null!;
	public DbSet<ProfileMemberMetadata> ProfileMemberMetadata { get; set; } = null!;
	public DbSet<Garden> Gardens { get; set; } = null!;
	public DbSet<PlayerData> PlayerData { get; set; } = null!;
	public DbSet<JacobData> JacobData { get; set; } = null!;
	public DbSet<JacobContest> JacobContests { get; set; } = null!;
	public DbSet<ContestParticipation> ContestParticipations { get; set; } = null!;
	public DbSet<Skills> Skills { get; set; } = null!;
	public DbSet<Farming> Farming { get; set; } = null!;
	public DbSet<ChocolateFactory> ChocolateFactories { get; set; } = null!;
	public DbSet<HypixelInventory> HypixelInventory { get; set; } = null!;
	public DbSet<HypixelItem> HypixelItems { get; set; } = null!;
	public DbSet<HypixelItemTexture> HypixelItemTextures { get; set; } = null!;

	// Discord
	public DbSet<Guild> Guilds { get; set; } = null!;
	public DbSet<GuildChannel> GuildChannels { get; set; } = null!;
	public DbSet<GuildRole> GuildRoles { get; set; } = null!;
	public DbSet<GuildMember> GuildMembers { get; set; } = null!;

	// Discord Monetization
	public DbSet<Product> Products { get; set; } = null!;
	public DbSet<Entitlement> Entitlements { get; set; } = null!;
	public DbSet<UserEntitlement> UserEntitlements { get; set; } = null!;
	public DbSet<GuildEntitlement> GuildEntitlements { get; set; } = null!;
	public DbSet<WeightStyle> WeightStyles { get; set; } = null!;
	public DbSet<ProductWeightStyle> ProductWeightStyles { get; set; } = null!;
	public DbSet<Image> Images { get; set; } = null!;
	public DbSet<Category> Categories { get; set; } = null!;
	public DbSet<ProductCategory> ProductCategories { get; set; } = null!;
	public DbSet<Tag> Tags { get; set; } = null!;
	public DbSet<ProductTag> ProductTags { get; set; } = null!;

	public DbSet<ShopOrder> ShopOrders { get; set; } = null!;
	public DbSet<ShopOrderItem> ShopOrderItems { get; set; } = null!;
	public DbSet<ProductAccess> ProductAccesses { get; set; } = null!;

	// Events
	public DbSet<Event> Events { get; set; } = null!;
	public DbSet<EventTeam> EventTeams { get; set; } = null!;
	public DbSet<EventMember> EventMembers { get; set; } = null!;
	public DbSet<WeightEvent> WeightEvents { get; set; } = null!;
	public DbSet<WeightEventMember> WeightEventMembers { get; set; } = null!;
	public DbSet<MedalEvent> MedalEvents { get; set; } = null!;
	public DbSet<MedalEventMember> MedalEventMembers { get; set; } = null!;
	public DbSet<PestEvent> PestEvents { get; set; } = null!;
	public DbSet<PestEventMember> PestEventMembers { get; set; } = null!;
	public DbSet<CollectionEvent> CollectionEvents { get; set; } = null!;
	public DbSet<CollectionEventMember> CollectionEventMembers { get; set; } = null!;

	// Timescale HyperTables
	public DbSet<SkillExperience> SkillExperiences { get; set; } = null!;
	public DbSet<CropCollection> CropCollections { get; set; } = null!;

	// Leaderboards
	public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
	public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; } = null!;
	public DbSet<LeaderboardSnapshot> LeaderboardSnapshots { get; set; } = null!;
	public DbSet<LeaderboardSnapshotEntry> LeaderboardSnapshotEntries { get; set; } = null!;

	// Bazaar
	public DbSet<BazaarProductSnapshot> BazaarProductSnapshots { get; set; } = null!;
	public DbSet<BazaarProductSummary> BazaarProductSummaries { get; set; } = null!;

	// Items
	public DbSet<SkyblockItem> SkyblockItems { get; set; } = null!;

	// Auction House
	public DbSet<AuctionBinPrice> AuctionBinPrices { get; set; } = null!;
	public DbSet<AuctionItem> AuctionItems { get; set; } = null!;
	public DbSet<Auction> Auctions { get; set; } = null!;
	public DbSet<AuctionPriceHistory> AuctionPriceHistories { get; set; } = null!;

	// Firesales
	public DbSet<SkyblockFiresale> SkyblockFiresales { get; set; } = null!;
	public DbSet<SkyblockFiresaleItem> SkyblockFiresaleItems { get; set; } = null!;

	// Annoucements 
	public DbSet<Announcement> Announcements { get; set; } = null!;
	public DbSet<DismissedAnnouncement> DismissedAnnouncements { get; set; } = null!;
	
	// Confirmations
	public DbSet<Confirmation> Confirmations { get; set; } = null!;
	public DbSet<UserConfirmation> UserConfirmations { get; set; } = null!;
	
	// Hypixel Guilds
	public DbSet<HypixelGuild> HypixelGuilds { get; set; } = null!;
	public DbSet<HypixelGuildMember> HypixelGuildMembers { get; set; } = null!;
	public DbSet<HypixelGuildMemberExp> HypixelGuildMemberExps { get; set; } = null!;
	public DbSet<HypixelGuildStats> HypixelGuildStats { get; set; } = null!;
	
	// Yearly Recap
	public DbSet<YearlyRecap> YearlyRecaps { get; set; } = null!;
	public DbSet<YearlyRecapSnapshot> YearlyRecapSnapshots { get; set; } = null!;

	// Community Guides
	public DbSet<Guide> Guides { get; set; } = null!;
	public DbSet<GuideVersion> GuideVersions { get; set; } = null!;
	public DbSet<GuideTag> GuideTags { get; set; } = null!;
	public DbSet<Comment> Comments { get; set; } = null!;
	public DbSet<CommentVote> CommentVotes { get; set; } = null!;
	public DbSet<GuideVote> GuideVotes { get; set; } = null!;
}