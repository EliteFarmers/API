namespace EliteAPI.Models.DTOs.Outgoing;

public class ProfileDto
{
    public required string ProfileId { get; set; }
    public required string ProfileName { get; set; }
    public string? GameMode { get; set; }
    public DateTime? LastSave { get; set; }
    public List<ProfileMemberDto> Members { get; set; } = new();
    public ProfileBankingDto Banking { get; set; } = new();
    public List<CraftedMinionDto> CraftedMinions { get; set; } = new();
    public bool IsDeleted { get; set; } = false;
}

public class ProfileMemberDto
{
    public required string ProfileId { get; set; }
    public required string PlayerUuid { get; set; }

    public List<CollectionDto> Collections { get; set; } = new();
    public required JacobDataDto Jacob { get; set; }
    public List<PetDto> Pets { get; set; } = new();
    public SkillsDto Skills { get; set; } = new();
    public bool IsSelected { get; set; }
    public bool WasRemoved { get; set; } = false;
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;
}