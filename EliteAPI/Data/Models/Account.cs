using System.ComponentModel.DataAnnotations;
using EliteAPI.Data.Models.Hypixel;

namespace EliteAPI.Data.Models;

public class Account
{
    [Key] public int Id { get; set; }
    public required DiscordAccount DiscordAccount { get; set; }

    public required Premium PremiumUser { get; set; }
    public List<MinecraftAccount> MinecraftAccounts { get; set; } = new();
}

public class MinecraftAccount
{
    [Key] public int MinecraftAccountId { get; set; }
    public required string Id { get; set; }
    public string UUID => Id;
    public required string Name { get; set; }
    public string IGN => Name;
    public required string Properties { get; set; }
    public List<Profile> Profiles { get; set; } = new();
    public PlayerData PlayerData { get; set; } = new();
}

public class DiscordAccount
{
    [Key] public ulong Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public string? Discriminator { get; set; }
    public string? Email { get; set; }
    public string? Locale { get; set; }
}