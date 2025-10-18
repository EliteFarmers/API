namespace EliteAPI.Models.DTOs.Outgoing;

public class YearlyContestsDto
{
	public int Year { get; set; }
	public int Count { get; set; }
	public bool Complete { get; set; }
	public Dictionary<long, List<string>> Contests { get; set; } = new();
}

public class YearlyCropRecordsDto
{
	public int Year { get; set; }
	public Dictionary<string, List<ContestParticipationWithTimestampDto>> Crops { get; set; } = new();
}

public class ContestParticipationWithTimestampDto
{
	public string PlayerUuid { get; set; } = "";
	public string PlayerName { get; set; } = "";
	public string ProfileUuid { get; set; } = "";
	public bool Removed { get; set; } = false;
	public long Timestamp { get; set; }
	public int Collected { get; set; } = 0;
	public int Position { get; set; } = -1;
	public int Participants { get; set; } = 0;
}