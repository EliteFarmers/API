using System.Text.Json.Serialization;
using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Models.DTOs.Outgoing;

public class LeaderboardDto
{
	public required string Id { get; set; }
	public required string Title { get; set; }
	public string? ShortTitle { get; set; }
	
	/// <summary>
	/// Item Id if this is a collection leaderboard
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ItemId { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Interval { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? FirstInterval { get; set; }

	public int Limit { get; set; }
	public int Offset { get; set; }
	public int MaxEntries { get; set; }

	/// <summary>
	/// The minimum score required to be on the leaderboard
	/// </summary>
	public decimal MinimumScore { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long StartsAt { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long EndsAt { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Profile { get; set; }

	public List<LeaderboardEntryDto> Entries { get; set; } = new();
}

public class LeaderboardEntryDto
{
	/// <summary>
	/// Player's IGN if player leaderboard
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Ign { get; init; }

	/// <summary>
	/// Player's profile name if player leaderboard
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Profile { get; set; }

	/// <summary>
	/// Uuid of the player or profile
	/// </summary>
	public required string Uuid { get; init; }

	/// <summary>
	/// Score of the entry
	/// </summary>
	public double Amount { get; init; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Removed { get; init; }

	/// <summary>
	/// Initial score of the entry
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double InitialAmount { get; set; }

	/// <summary>
	/// Game mode of the entry. Classic profiles are considered default/null.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Mode { get; set; }

	/// <summary>
	/// List of members if profile leaderboard
	/// </summary>
	[JsonIgnore]
	public List<ProfileLeaderboardMemberDto>? Members { get; set; }

	[JsonPropertyName("members")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ProfileLeaderboardMemberDto>? MembersSerializationHelper {
		get => Members?.Count > 0 ? Members : null;
		set => Members = value ?? null;
	}

	/// <summary>
	/// Metadata of the entry
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public MemberCosmeticsDto? Meta { get; set; }
}

public class LeaderboardEntryWithRankDto : LeaderboardEntryDto
{
	public int Rank { get; set; } = -1;
}

public class ProfileLeaderboardMemberDto
{
	public required string Ign { get; init; }
	public required string Uuid { get; init; }

	/// <summary>
	/// Skyblock xp of the player (used for sorting)
	/// </summary>
	public int Xp { get; init; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Removed { get; set; }
}

public class LeaderboardPositionsDto
{
	public Dictionary<string, int> Misc { get; set; } = new();
	public Dictionary<string, int> Skills { get; set; } = new();
	public Dictionary<string, int> Collections { get; set; } = new();
	public Dictionary<string, int> Pests { get; set; } = new();
	public Dictionary<string, int> Profile { get; set; } = new();
}

public class LeaderboardPositionDto
{
	/// <summary>
	/// Current rank of the player (-1 if not on leaderboard)
	/// </summary>
	public int Rank { get; set; }

	/// <summary>
	/// Current score of the player (0 if not on leaderboard)
	/// </summary>
	public double Amount { get; set; }

	/// <summary>
	/// The starting amount of the leaderboard entry for interval based leaderboards
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double InitialAmount { get; set; }

	/// <summary>
	/// The minimum amount required to be on the leaderboard. If this is a time based leaderboard,
	/// this score is instead required on the normal leaderboard before the player can be on the
	/// time based leaderboard
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double MinAmount { get; set; }

	/// <summary>
	/// The starting rank of the returned upcoming players list
	/// </summary>
	public int UpcomingRank { get; set; }

	/// <summary>
	/// List of upcoming players
	/// </summary>
	public List<LeaderboardEntryDto>? UpcomingPlayers { get; set; }

	/// <summary>
	/// List of previous players
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<LeaderboardEntryDto>? Previous { get; set; }
}