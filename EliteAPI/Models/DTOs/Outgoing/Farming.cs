using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.DTOs.Outgoing;

public class JacobDataDto
{
    public MedalInventoryDto Medals { get; set; } = new();
    public EarnedMedalInventoryDto EarnedMedals { get; set; } = new();
    public JacobPerksDto Perks { get; set; } = new();
    public JacobStatsDto Stats { get; set; } = new();
    
    public int Participations { get; set; }
    public int FirstPlaceScores { get; set; }
    public List<ContestParticipationDto> Contests { get; set; } = new();
}

public class MedalInventoryDto
{
    public int Bronze { get; set; } = 0;
    public int Silver { get; set; } = 0;
    public int Gold { get; set; } = 0;
}

public class EarnedMedalInventoryDto
{
    public int Bronze { get; set; } = 0;
    public int Silver { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public int Platinum { get; set; } = 0;
    public int Diamond { get; set; } = 0;
}

public class JacobPerksDto
{
    public int DoubleDrops { get; set; } = 0;
    public int LevelCap { get; set; } = 0;
}

public class JacobStatsDto 
{
    public Dictionary<Crop, ContestMedal> Brackets { get; set; } = new();
    public Dictionary<Crop, long> PersonalBests { get; set; } = new();
}

public class JacobContestDto
{
    public required string Crop { get; set; }
    public long Timestamp { get; set; }
    public int Participants { get; set; }
}

public class JacobContestWithParticipationsDto
{
    public required string Crop { get; set; }
    public long Timestamp { get; set; }
    public int Participants { get; set; }
    public List<StrippedContestParticipationDto> Participations { get; set; } = new();
}

public class StrippedContestParticipationDto {
    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public string? Medal { get; set; }
    public string PlayerUuid { get; set; } = "";
    public string PlayerName { get; set; } = "";
}

public class ContestParticipationDto
{
    public string Crop { get; set; } = "";
    public long Timestamp { get; set; }
    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public int Participants { get; set; } = 0;
    public string? Medal { get; set; }
}