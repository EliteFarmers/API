using System.ComponentModel;
using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Leaderboards.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards.Endpoints;

public class LeaderboardRequest
{
	/// <summary>
	/// Id of leaderboard
	/// </summary>
	public required string Leaderboard { get; set; }

	/// <summary>
	/// Time interval key of a monthly leaderboard. Format: yyyy-MM
	/// </summary>
	[QueryParam]
	[DefaultValue(null)]
	public string? Interval { get; set; } = null;

	/// <summary>
	/// Game mode to filter leaderboard by. Leave empty to get all modes.
	/// Options: "ironman", "island", "classic"
	/// </summary>
	[QueryParam]
	[DefaultValue(null)]
	public string? Mode { get; set; } = null;

	/// <summary>
	/// Removed filter to get leaderboard entries that have been removed from the leaderboard.
	/// Default is profiles that have not been removed/wiped.
	/// 0 = Not Removed
	/// 1 = Removed
	/// 2 = All
	/// </summary>
	[QueryParam]
	[DefaultValue(RemovedFilter.NotRemoved)]
	public RemovedFilter? Removed { get; set; } = RemovedFilter.NotRemoved;
}

internal sealed class LeaderboardRequestValidator : Validator<LeaderboardRequest>
{
	public LeaderboardRequestValidator() {
		var newLbService = Resolve<ILeaderboardRegistrationService>();

		RuleFor(x => x.Leaderboard)
			.NotEmpty()
			.WithMessage("Leaderboard is required")
			.When(x => newLbService.LeaderboardsById.ContainsKey(x.Leaderboard))
			.WithMessage("Leaderboard does not exist");

		RuleFor(x => x.Interval)
			.Matches(@"^\d{4}-W?\d{2}$")
			.When(x => x.Interval is not null)
			.WithMessage("Interval is invalid. Expected format: yyyy-MM or yyyy-Www");
	}
}