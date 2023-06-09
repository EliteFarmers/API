namespace EliteAPI.Models.DTOs.Outgoing;

public class ProfileDto
{
    public required string ProfileId { get; set; }
    public required string ProfileName { get; set; }
    public string? GameMode { get; set; }
    public DateTime? LastSave { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ProfileBankingDto Banking { get; set; } = new();
    public List<ProfileMemberDto> Members { get; set; } = new();
    public Dictionary<string, string> CraftedMinions { get; set; } = new();
}

public class ProfileMemberDto
{
    public required string ProfileId { get; set; }
    public required string PlayerUuid { get; set; }

    public int SkyblockXp { get; set; } = 0;
    public double Purse { get; set; } = 0;

    public Dictionary<string, long> Collections { get; set; } = new();
    public Dictionary<string, int> CollectionTiers { get; set; } = new();
    public Dictionary<string, int> CraftedMinions { get; set; } = new();
    public Dictionary<string, double> Stats { get; set; } = new();
    public Dictionary<string, int> Essence { get; set; } = new();

    public required JacobDataDto Jacob { get; set; }
    public List<PetDto> Pets { get; set; } = new();
    public SkillsDto Skills { get; set; } = new();
    public bool IsSelected { get; set; } = false;
    public bool WasRemoved { get; set; } = false;
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;
}