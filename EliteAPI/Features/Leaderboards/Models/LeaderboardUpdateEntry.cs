namespace EliteAPI.Features.Leaderboards.Models;

/// <summary>
/// Represents a leaderboard update operation to be batched.
/// </summary>
public record LeaderboardUpdateEntry {
	/// <summary>
	/// Database ID of the leaderboard
	/// </summary>
	public required int LeaderboardId { get; init; }
	
	/// <summary>
	/// For member leaderboards - the profile member's ID
	/// </summary>
	public Guid? ProfileMemberId { get; init; }
	
	/// <summary>
	/// For profile leaderboards - the profile ID
	/// </summary>
	public string? ProfileId { get; init; }
	
	/// <summary>
	/// The calculated score for this leaderboard entry
	/// </summary>
	public required decimal Score { get; init; }
	
	/// <summary>
	/// The initial score (used for interval-based increase calculations)
	/// </summary>
	public required decimal InitialScore { get; init; }
	
	/// <summary>
	/// Null for Current leaderboard, otherwise the interval identifier (e.g. "2026-01", "2026-W05")
	/// </summary>
	public string? IntervalIdentifier { get; init; }
	
	/// <summary>
	/// Whether this entry belongs to a removed/deleted profile member
	/// </summary>
	public required bool IsRemoved { get; init; }
	
	/// <summary>
	/// Profile type/game mode (e.g. "ironman", "island", null for classic)
	/// </summary>
	public string? ProfileType { get; init; }
	
	/// <summary>
	/// The operation to perform on this entry
	/// </summary>
	public required LeaderboardUpdateOperation Operation { get; init; }
	
	/// <summary>
	/// Existing entry ID if this is an update operation
	/// </summary>
	public int? ExistingEntryId { get; init; }
}

/// <summary>
/// The type of operation to perform on a leaderboard entry
/// </summary>
public enum LeaderboardUpdateOperation {
	/// <summary>
	/// Insert a new entry
	/// </summary>
	Insert,
	
	/// <summary>
	/// Update an existing entry
	/// </summary>
	Update,
	
	/// <summary>
	/// Delete an existing entry
	/// </summary>
	Delete
}
