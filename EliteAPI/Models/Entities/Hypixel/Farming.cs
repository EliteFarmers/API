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
    public MedalInventory EarnedMedals { get; set; } = new();
    public JacobPerks Perks { get; set; } = new();
    public int Participations { get; set; } = 0;
    public virtual List<ContestParticipation> Contests { get; set; } = new();
    public DateTime ContestsLastUpdated { get; set; } = DateTime.MinValue.ToUniversalTime();

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
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
public class JacobPerks
{
    public int DoubleDrops { get; set; } = 0;
    public int LevelCap { get; set; } = 0;
}

public class JacobContestEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }
    public List<JacobContest> JacobContests { get; set; } = new();
}

[Index(nameof(Timestamp))]
public class JacobContest
{
    [Key]
    public long Id { get; set; }

    public Crop Crop { get; set; }
    public long Timestamp { get; set; }
    public int Participants { get; set; }

    public List<ContestParticipation> Participations { get; set; } = new();
}

public class ContestParticipation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public ContestMedal MedalEarned { get; set; } = ContestMedal.None;

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember ProfileMember { get; set; } = null!;

    [ForeignKey("JacobContest")]
    public long JacobContestId { get; set; }
    public JacobContest JacobContest { get; set; } = null!;
}

public enum Crop
{
    Cactus,
    Carrot,
    CocoaBeans,
    Melon,
    Mushroom,
    NetherWart,
    Potato,
    Pumpkin,
    SugarCane,
    Wheat
}

public enum ContestMedal
{
    None,
    Bronze,
    Silver,
    Gold
}