namespace EliteAPI.Models.DTOs.Outgoing;

public class ProfileDetailsDto
{
    public required string ProfileId { get; set; }
    public required string ProfileName { get; set; }
    public string GameMode { get; set; } = "classic";
    public bool Selected { get; set; }
    public double BankBalance { get; set; }
    public List<MemberDetailsDto> Members { get; set; } = new();
}

public class MemberDetailsDto
{
    public required string Uuid { get; set; }
    public required string Username { get; set; }
    public bool Active { get; set; } = true;
}

public class ProfileMemberDto
{
    public required string ProfileId { get; set; }
    public required string PlayerUuid { get; set; }

    public int SkyblockXp { get; set; } = 0;
    public double Purse { get; set; } = 0;
    public double BankBalance { get; set; } = 0;

    public Dictionary<string, long> Collections { get; set; } = new();
    public Dictionary<string, int> CollectionTiers { get; set; } = new();
    public Dictionary<string, int> CraftedMinions { get; set; } = new();
    // public Dictionary<string, double> Stats { get; set; } = new(); // Currently unused
    // public Dictionary<string, int> Essence { get; set; } = new();
    public List<PetDto> Pets { get; set; } = new();

    public required JacobDataDto Jacob { get; set; }
    public required FarmingWeightDto FarmingWeight { get; set; }
    public SkillsDto Skills { get; set; } = new();

    public bool IsSelected { get; set; } = false;
    public bool WasRemoved { get; set; } = false;
    public long LastUpdated { get; set; } = 0;
}