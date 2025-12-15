using System.Text.Json.Serialization;

namespace EliteAPI.Configuration.Settings;

// ReSharper disable once ClassNeverInstantiated.Global
public class Leaderboard
{
	public required string Id { get; set; }
	public required string Title { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ItemId { get; set; }
	public int Limit { get; set; } = 1000;
	public required string Order { get; set; } = "desc";
	public int ScoreFormat { get; set; } = 1;
	public bool Profile { get; set; } = false;
}