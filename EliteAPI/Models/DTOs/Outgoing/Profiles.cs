namespace EliteAPI.Models.DTOs.Outgoing;

public class ProfileDto
{
    public required string ProfileId { get; set; }
    public required string ProfileName { get; set; }
    public string GameMode { get; set; } = "classic";
    public long LastSave { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ProfileBankingDto Banking { get; set; } = new();
    public List<MemberDetailsDto> Members { get; set; } = new();
    public Dictionary<string, string> CraftedMinions { get; set; } = new();
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
    public SkillsDto Skills { get; set; } = new();
    public bool IsSelected { get; set; } = false;
    public bool WasRemoved { get; set; } = false;
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;
}