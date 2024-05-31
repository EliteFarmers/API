using EliteAPI.Data.Configurations;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Z.EntityFramework.Extensions;

namespace EliteAPI.Data;

public class DataContext(DbContextOptions<DataContext> options, IConfiguration config) : IdentityDbContext<ApiUser>(options) 
{
    private static NpgsqlDataSource? Source { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        EntityFrameworkManager.IsCommunity = true;
        
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
        
        optionsBuilder.UseNpgsql(Source);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        
        modelBuilder.HasCollation("case_insensitive", locale: "en-u-ks-primary", provider: "icu", deterministic: false);
        
        modelBuilder.Entity<MinecraftAccount>().Property(c => c.Name)
            .UseCollation("case_insensitive");

        modelBuilder.Entity<EliteAccount>().Navigation(e => e.MinecraftAccounts).AutoInclude();
        modelBuilder.Entity<MinecraftAccount>().Navigation(e => e.Badges).AutoInclude();
        modelBuilder.Entity<UserBadge>().Navigation(e => e.Badge).AutoInclude();

        modelBuilder.Entity<Event>().HasDiscriminator(e => e.Type)
            .HasValue<Event>(EventType.None)
            .HasValue<WeightEvent>(EventType.FarmingWeight)
            .HasValue<MedalEvent>(EventType.Medals);

        modelBuilder.Entity<EventMember>().HasDiscriminator(e => e.Type)
            .HasValue<EventMember>(EventType.None)
            .HasValue<WeightEventMember>(EventType.FarmingWeight)
            .HasValue<MedalEventMember>(EventType.Medals);
    }

    public DbSet<EliteAccount> Accounts { get; set; } = null!;
    public DbSet<MinecraftAccount> MinecraftAccounts { get; set; } = null!;
    public DbSet<Badge> Badges { get; set; } = null!;
    public DbSet<UserBadge> UserBadges { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<ProfileMember> ProfileMembers { get; set; } = null!;
    public DbSet<PlayerData> PlayerData { get; set; } = null!;
    public DbSet<JacobData> JacobData { get; set; } = null!;
    public DbSet<JacobContest> JacobContests { get; set; } = null!;
    public DbSet<ContestParticipation> ContestParticipations { get; set; } = null!;
    public DbSet<Skills> Skills { get; set; } = null!;
    public DbSet<Farming> Farming { get; set; } = null!;
    public DbSet<ChocolateFactory> ChocolateFactories { get; set; } = null!;

    // Discord
    public DbSet<Guild> Guilds { get; set; } = null!;

    // Events
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<WeightEvent> WeightEvents { get; set; } = null!;
    public DbSet<MedalEvent> MedalEvents { get; set; } = null!;
    public DbSet<EventMember> EventMembers { get; set; } = null!;
    public DbSet<WeightEventMember> WeightEventMembers { get; set; } = null!;
    public DbSet<MedalEventMember> MedalEventMembers { get; set; } = null!;

    // Timescale HyperTables
    public DbSet<SkillExperience> SkillExperiences { get; set; } = null!;
    public DbSet<CropCollection> CropCollections { get; set; } = null!;
}
