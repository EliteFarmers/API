using System.ComponentModel.DataAnnotations;

namespace EliteAPI.Models.DTOs.Outgoing;

public class AccountDto
{
    public required DiscordAccountDto DiscordAccount { get; set; }

    public PremiumDto? PremiumUser { get; set; }
    public List<MinecraftAccountDto> MinecraftAccounts { get; set; } = new();
}

public class MinecraftAccountDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public List<MinecraftAccountPropertyDto> Properties { get; set; } = new();
    public List<ProfileDto> Profiles { get; set; } = new();
    // public PlayerData PlayerData { get; set; } = new();
}

public class DiscordAccountDto
{
    public ulong Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public string? Discriminator { get; set; }
    public string? Email { get; set; }
    public string? Locale { get; set; }
}

public class MinecraftAccountPropertyDto
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}