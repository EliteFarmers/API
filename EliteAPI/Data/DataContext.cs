using EliteAPI.Data.Models.Hypixel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EliteAPI.Data;
public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    protected override async void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Get connection string from secrets
        string connection = Environment.GetEnvironmentVariable("PostgresConnection") ?? "Server=database;Port=5432;Database=eliteapi;Username=user;Password=postgres123";

        if (!string.IsNullOrEmpty(connection))
        {
            optionsBuilder.UseNpgsql(connection);
        }
        else
        {
            // Quit
            Console.WriteLine("No connection string found. Quitting...");
            Environment.Exit(1);
        }
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<DiscordAccount> DiscordAccounts { get; set; }
    public DbSet<MinecraftAccount> MinecraftAccounts { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<PlayerData> PlayerData { get; set; }
    public DbSet<Premium> PremiumUsers { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Collection> Collections { get; set; }
}
