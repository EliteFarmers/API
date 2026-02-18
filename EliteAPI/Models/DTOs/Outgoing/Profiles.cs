using System.Text.Json.Serialization;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Profiles.Models;

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

public class ProfileMemberNetworthDto
{
	public double Normal { get; set; } = 0;
	public double Liquid { get; set; } = 0;
	public double Functional { get; set; } = 0;
	public double LiquidFunctional { get; set; } = 0;
}

public class ProfileMemberDto
{
	public required string ProfileId { get; set; }
	public required string PlayerUuid { get; set; }
	public required string ProfileName { get; set; }

	public ApiAccessDto Api { get; set; } = new();

	public int SkyblockXp { get; set; }
	public double SocialXp { get; set; }
	public double Purse { get; set; }
	public double BankBalance { get; set; }
	public double PersonalBank { get; set; } = 0;
	public ProfileMemberNetworthDto Networth { get; set; } = new();

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public MemberCosmeticsDto? Meta { get; set; }

	public Dictionary<string, long> Collections { get; set; } = new();
	public Dictionary<string, int> CollectionTiers { get; set; } = new();
	public Dictionary<string, int> CraftedMinions { get; set; } = new();
	public Dictionary<string, long> Sacks { get; set; } = new();
	public List<PetDto> Pets { get; set; } = [];
	public UnparsedApiDataDto Unparsed { get; set; } = new();
	public required JacobDataDto Jacob { get; set; }
	public required FarmingWeightDto FarmingWeight { get; set; }
	public GardenDto? Garden { get; set; }
	public SkillsDto Skills { get; set; } = new();
	public ProfileMemberDataDto MemberData { get; set; } = new();
	public ChocolateFactoryDto ChocolateFactory { get; set; } = new();
	public List<ProfileEventMemberDto> Events { get; set; } = [];
	public List<HypixelInventoryOverviewDto> Inventories { get; set; } = [];

	public bool IsSelected { get; set; }
	public bool WasRemoved { get; set; }
	public long LastUpdated { get; set; }
	public long LastDataChanged { get; set; }
}

public class ProfileMemberDataDto
{
	public Dictionary<string, int> AttributeStacks { get; set; } = new();
	public List<ProfileMemberShardDataDto> Shards { get; set; } = [];
	public ProfileMemberGardenChipsDataDto GardenChips { get; set; } = new();
	public Dictionary<string, ProfileMemberMutationDataDto> Mutations { get; set; } = new();
}

public class ProfileMemberShardDataDto
{
	public required string Type { get; set; }
	public int AmountOwned { get; set; }
	public long CapturedAt { get; set; }
}

public class ProfileMemberGardenChipsDataDto
{
	public int? Cropshot { get; set; }
	public int? Sowledge { get; set; }
	public int? Hypercharge { get; set; }
	public int? Quickdraw { get; set; }
	public int? Mechamind { get; set; }
	public int? Overdrive { get; set; }
	public int? Synthesis { get; set; }
	public int? VerminVaporizer { get; set; }
	public int? Evergreen { get; set; }
	public int? Rarefinder { get; set; }
}

public class ProfileMemberMutationDataDto
{
	public bool Analyzed { get; set; }
	public bool Discovered { get; set; }
}
