using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class JacobData
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public MedalInventory Medals { get; set; } = new();
	public EarnedMedalInventory EarnedMedals { get; set; } = new();
	public JacobPerks Perks { get; set; } = new();
	public int Participations { get; set; }
	public int FirstPlaceScores { get; set; }

	[Column(TypeName = "jsonb")] public JacobStats? Stats { get; set; } = new();

	public virtual List<ContestParticipation> Contests { get; set; } = new();
	public long ContestsLastUpdated { get; set; }

	[ForeignKey("ProfileMember")] public Guid ProfileMemberId { get; set; }
	public ProfileMember? ProfileMember { get; set; }
}

[Owned]
public class MedalInventory
{
	public int Bronze { get; set; } = 0;
	public int Silver { get; set; } = 0;
	public int Gold { get; set; } = 0;
}

[Owned]
public class EarnedMedalInventory
{
	public int Bronze { get; set; } = 0;
	public int Silver { get; set; } = 0;
	public int Gold { get; set; } = 0;
	public int Platinum { get; set; } = 0;
	public int Diamond { get; set; } = 0;
}

[Owned]
public class JacobPerks
{
	public int DoubleDrops { get; set; } = 0;
	public int LevelCap { get; set; } = 0;
	public bool PersonalBests { get; set; } = false;
}

public class JacobStats
{
	public Dictionary<Crop, ContestMedal> Brackets { get; set; } = new();
	public Dictionary<Crop, long> PersonalBests { get; set; } = new();
	public Dictionary<Crop, JacobCropStats> Crops { get; set; } = new();
}

public class JacobCropStats
{
	public int Participations { get; set; }
	public int FirstPlaceScores { get; set; }
	public long? PersonalBestTimestamp { get; set; }
	public EarnedMedalInventory Medals { get; set; } = new();
}

[Index(nameof(Timestamp))]
public class JacobContest
{
	[Key] public long Id { get; set; }

	public Crop Crop { get; set; }
	public long Timestamp { get; set; }
	public int Participants { get; set; }
	public bool Finnegan { get; set; }

	public int Bronze { get; set; }
	public int Silver { get; set; }
	public int Gold { get; set; }
	public int Platinum { get; set; }
	public int Diamond { get; set; }
}

public class ContestParticipation
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public int Collected { get; set; }
	public int Position { get; set; } = -1;
	public ContestMedal MedalEarned { get; set; } = ContestMedal.None;

	[ForeignKey("ProfileMember")] public Guid ProfileMemberId { get; set; }
	public ProfileMember ProfileMember { get; set; } = null!;

	[ForeignKey("JacobContest")] public long JacobContestId { get; set; }
	public JacobContest JacobContest { get; set; } = null!;
}

public enum Crop
{
	Cactus = 0,
	Carrot = 1,
	CocoaBeans = 2,
	Melon = 3,
	Mushroom = 4,
	NetherWart = 5,
	Potato = 6,
	Pumpkin = 7,
	SugarCane = 8,
	Wheat = 9,
	Seeds = 10 // Only used in some scenarios 
}

public enum Pest
{
	Mite = 0,
	Cricket = 1,
	Moth = 2,
	Earthworm = 3,
	Slug = 4,
	Beetle = 5,
	Locust = 6,
	Rat = 7,
	Mosquito = 8,
	Fly = 9,
	Mouse = 10
}

public enum ContestMedal
{
	Unclaimable = -1,
	None = 0,
	Bronze = 1,
	Silver = 2,
	Gold = 3,
	Platinum = 4,
	Diamond = 5
}