using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models.Hypixel;

public class JacobData
{
    [Key] public int Id { get; set; }
    public required MedalInventory Medals { get; set; }
    public required MedalInventory EarnedMedals { get; set; }
    public required JacobPerks Perks { get; set; }
    public int Participations { get; set; } = 0;
    public List<ContestParticipation> Contests { get; set; } = new();

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
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
    [Key] public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public List<JacobContest> JacobContests { get; set; } = new();
}

public class JacobContest
{
    [Key] public int Id { get; set; }
    public Crop Crop { get; set; }
    public DateTime Timestamp { get; set; }
    public int Participants => Participations.Count;
    public List<ContestParticipation> Participations { get; set; } = new();

    [ForeignKey("JacobContestEvent")]
    public int JacobContestEventId { get; set; }
    public required JacobContestEvent JacobContestEvent { get; set; }
}

public class ContestParticipation {
    [Key] public int Id { get; set; }
    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public ContestMedal MedalEarned { get; set; } = ContestMedal.None;

    [ForeignKey("JacobContest")]
    public int JacobContestId { get; set; }
    public required JacobContest JacobContest { get; set; }

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public required ProfileMember ProfileMember { get; set; }
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