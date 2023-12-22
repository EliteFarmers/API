using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Z.EntityFramework.Extensions;

namespace EliteAPI.Data;
public class DataContext(DbContextOptions<DataContext> options, IConfiguration config) : DbContext(options) 
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
        modelBuilder.HasCollation("case_insensitive", locale: "en-u-ks-primary", provider: "icu", deterministic: false);
        
        modelBuilder.Entity<MinecraftAccount>().Property(c => c.Name)
            .UseCollation("case_insensitive");
    }

    public DbSet<EliteAccount> Accounts { get; set; } = null!;
    public DbSet<MinecraftAccount> MinecraftAccounts { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<ProfileMember> ProfileMembers { get; set; } = null!;
    public DbSet<PlayerData> PlayerData { get; set; } = null!;
    public DbSet<JacobData> JacobData { get; set; } = null!;
    public DbSet<JacobContest> JacobContests { get; set; } = null!;
    public DbSet<ContestParticipation> ContestParticipations { get; set; } = null!;
    public DbSet<Skills> Skills { get; set; } = null!;
    public DbSet<Farming> Farming { get; set; } = null!;

    // Discord
    public DbSet<Guild> Guilds { get; set; } = null!;

    // Events
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<EventMember> EventMembers { get; set; } = null!;

    // Timescale HyperTables
    public DbSet<SkillExperience> SkillExperiences { get; set; } = null!;
    public DbSet<CropCollection> CropCollections { get; set; } = null!;
}
