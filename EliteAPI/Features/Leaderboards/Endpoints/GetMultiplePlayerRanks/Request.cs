using System.ComponentModel;
using System.Text.Json.Serialization;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetMultiplePlayerRanks;

public class GetMultiplePlayerRanksRequest : PlayerProfileUuidRequest {
	/// <summary>
	/// Ids of leaderboards (comma-separated)
	/// </summary>
	[QueryParam]
	public required string Leaderboards { get; set; }

	[JsonIgnore] public List<string> LeaderboardList => Leaderboards.ToLowerInvariant().Split(',').ToList();
	
	/// <summary>
	/// Include upcoming players
	/// </summary>
	[QueryParam, DefaultValue(false), Obsolete("Use Upcoming instead")]
	public bool? IncludeUpcoming { get; set; } = false;
	
	/// <summary>
	/// Amount of upcoming players to include (max 100). Only works with new leaderboard backend
	/// </summary>
	[QueryParam, DefaultValue(0)]
	public int? Upcoming { get; set; } = 0;
	
	/// <summary>
	/// Start at a specified rank for upcoming players
	/// </summary>
	[QueryParam]
	public int? AtRank { get; set; } = -1;
	
	/// <summary>
	/// Time interval key of a monthly leaderboard. Format: yyyy-MM
	/// </summary>
	[QueryParam, DefaultValue(null)]
	public string? Interval { get; set; } = null;
	
	/// <summary>
	/// Game mode to filter leaderboard by. Leave empty to get all modes.
	/// Options: "ironman", "island", "classic"
	/// </summary>
	[QueryParam, DefaultValue(null)]
	public string? Mode { get; set; } = null;
	
	/// <summary>
	/// Removed filter to get leaderboard entries that have been removed from the leaderboard.
	/// Default is profiles that have not been removed/wiped.
	/// 0 = Not Removed
	/// 1 = Removed
	/// 2 = All
	/// </summary>
	[QueryParam, DefaultValue(RemovedFilter.NotRemoved)]
	public RemovedFilter? Removed { get; set; } = RemovedFilter.NotRemoved;
}

internal sealed class GetMultiplePlayerRanksRequestValidator : Validator<GetMultiplePlayerRanksRequest> {
	public GetMultiplePlayerRanksRequestValidator() {
		Include(new PlayerProfileUuidRequestValidator());
		
		var newLbService = Resolve<ILeaderboardRegistrationService>();
		RuleFor(x => x.Leaderboards)
			.NotEmpty()
			.WithMessage("Leaderboards are required");

		RuleForEach(x => x.LeaderboardList)
			.NotEmpty()
			.WithMessage("Leaderboard cannot be empty")
			.Must(newLbService.LeaderboardsById.ContainsKey)
			.WithMessage("Leaderboard does not exist");
		
		RuleFor(x => x.Upcoming)
			.GreaterThanOrEqualTo(0)
			.LessThanOrEqualTo(10)
			.WithMessage("Upcoming must be between 0 and 10");
		
		RuleFor(x => x.Interval)
			.Matches(@"^\d{4}-\d{2}$")
			.When(x => x.Interval is not null)
			.WithMessage("Interval is invalid. Expected format: yyyy-MM");
	}
}