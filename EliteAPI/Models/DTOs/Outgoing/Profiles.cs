﻿using System.Text.Json.Serialization;
using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Models.DTOs.Outgoing;

public class ProfileDetailsDto
{
    public required string ProfileId { get; set; }
    public required string ProfileName { get; set; }
    public string GameMode { get; set; } = "classic";
    public bool Selected { get; set; }
    public double BankBalance { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Deleted { get; set; } = false;
    public List<MemberDetailsDto> Members { get; set; } = new();
}

public class ProfileNamesDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Selected { get; set; }
}

public class MemberDetailsDto
{
    public required string Uuid { get; set; }
    public required string Username { get; set; }
    public string? ProfileName { get; set; }
    public bool Active { get; set; } = true;
    public double FarmingWeight { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MemberCosmeticsDto? Meta { get; set; }
}

public class ProfileMemberDto
{
    public required string ProfileId { get; set; }
    public required string PlayerUuid { get; set; }
    public required string ProfileName { get; set; }
    
    public ApiAccessDto Api { get; set; } = new();
    
    public int SkyblockXp { get; set; }
    public double Purse { get; set; }
    public double BankBalance { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MemberCosmeticsDto? Meta { get; set; }

    public Dictionary<string, long> Collections { get; set; } = new();
    public Dictionary<string, int> CollectionTiers { get; set; } = new();
    public Dictionary<string, int> CraftedMinions { get; set; } = new();
    public List<PetDto> Pets { get; set; } = [];
    
    // public InventoriesDto Inventories { get; set; } = new();
    public UnparsedApiDataDto Unparsed { get; set; } = new();

    public required JacobDataDto Jacob { get; set; }
    public required FarmingWeightDto FarmingWeight { get; set; }
    public GardenDto? Garden { get; set; }
    public SkillsDto Skills { get; set; } = new();
    public ChocolateFactoryDto ChocolateFactory { get; set; } = new();
    public List<ProfileEventMemberDto> Events { get; set; } = [];

    public bool IsSelected { get; set; }
    public bool WasRemoved { get; set; }
    public long LastUpdated { get; set; }
}