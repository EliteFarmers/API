using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Hypixel;
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
        }
        else
        {
            // Quit
            Console.WriteLine("No connection string found. Quitting...");
            //Environment.Exit(1);
        }
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<MinecraftAccount> MinecraftAccounts { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<ProfileMember> ProfileMembers { get; set; } = null!;
    public DbSet<PlayerData> PlayerData { get; set; } = null!;
    public DbSet<Premium> PremiumUsers { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<JacobData> JacobData { get; set; } = null!;
    public DbSet<JacobContest> JacobContests { get; set; } = null!;
    public DbSet<ContestParticipation> ContestParticipations { get; set; } = null!;
    public DbSet<JacobContestEvent> JacobContestEvents { get; set; } = null!;
    public DbSet<Pet> Pets { get; set; } = null!;
    public DbSet<Skills> Skills { get; set; } = null!;
    public DbSet<ProfileBanking> ProfileBanking { get; set; } = null!;
}
