using System.Text.Json.Serialization;
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
    public Dictionary<Crop, JacobCropStatsDto> Crops { get; set; } = new();
}

public class JacobContestDto
{
    public required string Crop { get; set; }
    public long Timestamp { get; set; }
    public int Participants { get; set; }
    public ContestBracketsDto Brackets { get; set; } = new();
}

public class JacobCropStatsDto {
    public int Participations { get; set; }
    public int FirstPlaceScores { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PersonalBestTimestamp { get; set; }
    public EarnedMedalInventoryDto Medals { get; set; } = new();
}

public class JacobContestWithParticipationsDto
{
    public required string Crop { get; set; }
    public long Timestamp { get; set; }
    public int Participants { get; set; }
    public ContestBracketsDto Brackets { get; set; } = new();
    public List<StrippedContestParticipationDto> Participations { get; set; } = new();
}

public class StrippedContestParticipationDto {
    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public string? Medal { get; set; }
    public bool Removed { get; set; } = false;
    public string PlayerUuid { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string ProfileUuid { get; set; } = "";
}

public class ContestBracketsDetailsDto {
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public Dictionary<string, ContestBracketsDto> Brackets { get; set; } = new();
}

public class ContestBracketsDto {
    public int Bronze { get; set; }
    public int Silver { get; set; }
    public int Gold { get; set; }
    public int Platinum { get; set; }
    public int Diamond { get; set; }
}

public class ContestParticipationDto
{
    public string Crop { get; set; } = "";
    /// <summary>
    /// Timestamp of the contest in seconds since unix epoch.
    /// </summary>
    public long Timestamp { get; set; }
    public int Collected { get; set; } = 0;
    public int Position { get; set; } = -1;
    public int Participants { get; set; } = 0;
    public string? Medal { get; set; }
}

public class MedalCutoffsDbDto {
    public int Crop { get; set; } = -1;
    public double? Bronze { get; set; } = -1;
    public double? Silver { get; set; } = -1;
    public double? Gold { get; set; } = -1;
    public double? Platinum { get; set; } = -1;
    public double? Diamond { get; set; } = -1;
}