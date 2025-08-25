using System.ComponentModel;
using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetProfileRank;

public class GetProfileRankRequest : ProfileUuidRequest {
	/// <summary>
	/// Id of leaderboard
	/// </summary>
	public required string Leaderboard { get; set; }
	
	/// <summary>
	/// Include upcoming players
	/// </summary>
	[QueryParam, DefaultValue(false)]
	public bool? IncludeUpcoming { get; set; } = false;
	
	/// <summary>
	/// Amount of upcoming players to include (max 100).
	/// </summary>
	[QueryParam, DefaultValue(0)]
	public int? Upcoming { get; set; } = 0;
	
	/// <summary>
	/// Amount of passed players to include (max 3).
	/// </summary>
	[QueryParam, DefaultValue(0)]
	public int? Previous { get; set; } = 0;
	
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

internal sealed class GetProfileRankRequestValidator : Validator<GetProfileRankRequest> {
	public GetProfileRankRequestValidator() {
		Include(new ProfileUuidRequestValidator());
		
		var newLbService = Resolve<ILeaderboardRegistrationService>();
		RuleFor(x => x.Leaderboard)
			.NotEmpty()
			.WithMessage("Leaderboard is required")
			.When(x => newLbService.LeaderboardsById.ContainsKey(x.Leaderboard))
			.WithMessage("Leaderboard does not exist");
		
		RuleFor(x => x.Upcoming)
			.GreaterThanOrEqualTo(0)
			.LessThanOrEqualTo(20)
			.WithMessage("Upcoming must be between 0 and 20");
				
		RuleFor(x => x.Previous)
			.GreaterThanOrEqualTo(0)
			.LessThanOrEqualTo(3)
			.WithMessage("Previous must be between 0 and 3");
				
		RuleFor(x => x.Interval)
			.Matches(@"^\d{4}-\d{2}$")
			.When(x => x.Interval is not null)
			.WithMessage("Interval is invalid. Expected format: yyyy-MM");
	}
}