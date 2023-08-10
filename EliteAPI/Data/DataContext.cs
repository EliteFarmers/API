using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Data;
public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Get connection string from secrets
        var connection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");

        if (!string.IsNullOrEmpty(connection))
        {
            optionsBuilder.UseNpgsql(connection);
            optionsBuilder.EnableSensitiveDataLogging();
        }
        else
        {
            // Quit
            Console.WriteLine("No connection string found. Quitting...");
            //Environment.Exit(1);
        }
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
    public DbSet<Inventories> Inventories { get; set; } = null!;
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
