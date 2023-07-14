using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NuGet.Protocol;

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

    public DbSet<AccountEntity> Accounts { get; set; } = null!;
    public DbSet<MinecraftAccount> MinecraftAccounts { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<ProfileMember> ProfileMembers { get; set; } = null!;
    public DbSet<PlayerData> PlayerData { get; set; } = null!;
    public DbSet<JacobData> JacobData { get; set; } = null!;
    public DbSet<JacobContest> JacobContests { get; set; } = null!;
    public DbSet<ContestParticipation> ContestParticipations { get; set; } = null!;
    public DbSet<Skills> Skills { get; set; } = null!;
    public DbSet<FarmingWeight> FarmingWeights { get; set; } = null!;
    
    // Timescale HyperTables
    public DbSet<SkillExperience> SkillExperiences { get; set; } = null!;
    public DbSet<CropCollection> CropCollections { get; set; } = null!;
}
