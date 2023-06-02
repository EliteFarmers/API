using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.DTOs.Outgoing;

public class JacobDataDto
{
    public MedalInventoryDto Medals { get; set; } = new();
    public MedalInventoryDto EarnedMedals { get; set; } = new();
    public JacobPerksDto Perks { get; set; } = new();
    public int Participations { get; set; } = 0;
    public List<ContestParticipationDto> Contests { get; set; } = new();
}

public class MedalInventoryDto
{
    public int Bronze { get; set; } = 0;
    public int Silver { get; set; } = 0;
    public int Gold { get; set; } = 0;
}

public class JacobPerksDto
{
    public int DoubleDrops { get; set; } = 0;
    public int LevelCap { get; set; } = 0;
}

public class JacobContestEventDto
{
    public DateTime Timestamp { get; set; }
    public List<JacobContestDto> JacobContests { get; set; } = new();
}

public class JacobContestDto
{
    public Crop Crop { get; set; }
    public DateTime Timestamp { get; set; }
    public int Participants => Participations.Count;
    public List<ContestParticipationDto> Participations { get; set; } = new();
}

public class ContestParticipationDto
{
    public string Crop { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public int Participants { get; set; } = 0;
    public ContestMedal MedalEarned { get; set; } = ContestMedal.None;
}