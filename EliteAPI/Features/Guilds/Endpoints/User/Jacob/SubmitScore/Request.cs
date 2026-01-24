using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guilds.User.Jacob.SubmitScore;

public class SubmitScoreRequest : DiscordIdRequest
{
	public required string LeaderboardId { get; set; }
	
	/// <summary>
	/// Discord User ID of the submitter. Required when called by Bot.
	/// If not provided, the authenticated user's Discord ID is used.
	/// </summary>
	[QueryParam]
	public long? DiscordUserId { get; set; }
	
	/// <summary>
	/// Optional role IDs of the submitter. If provided, these are used for role checks.
	/// If not provided, the backend will fetch the user's roles from stored guild member data.
	/// </summary>
	[FromBody] public List<string>? UserRoleIds { get; set; }
}

public class SubmitScoreResponse
{
	public List<ScoreChangeDto> Changes { get; set; } = [];
	public bool HasNewRecords => Changes.Count > 0;
	public bool ShouldPing { get; set; }
}

public class ScoreChangeDto
{
	public required string Crop { get; set; }
	public int NewPosition { get; set; }
	public int OldPosition { get; set; } = -1;
	
	public required SubmitterInfoDto Submitter { get; set; }
	public required ContestParticipationDto Record { get; set; }
	
	/// <summary>
	/// Information about the previous record holder at this position, if any.
	/// </summary>
	public DisplacedEntryDto? DisplacedEntry { get; set; }
	
	/// <summary>
	/// Information about the entry that was knocked out of the leaderboard (e.g., 3rd place -> off).
	/// </summary>
	public DisplacedEntryDto? KnockedOutEntry { get; set; }
	
	/// <summary>
	/// The amount the submitter improved their own score (if applicable).
	/// </summary>
	public int? Improvement { get; set; }
	
	public bool IsNewHighScore => NewPosition == 0;
	public bool IsImprovement => OldPosition != -1 && Improvement > 0;
}

public class SubmitterInfoDto
{
	public required string Uuid { get; set; }
	public required string Ign { get; set; }
	public required string DiscordId { get; set; }
}

public class DisplacedEntryDto
{
	public required string Uuid { get; set; }
	public required string Ign { get; set; }
	public required string DiscordId { get; set; }
	public int Collected { get; set; }
	public int PreviousPosition { get; set; }
}

internal sealed class SubmitScoreRequestValidator : Validator<SubmitScoreRequest>
{
	public SubmitScoreRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.LeaderboardId)
			.NotEmpty()
			.WithMessage("LeaderboardId is required");
	}
}
