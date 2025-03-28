using System.ComponentModel;
using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Leaderboards.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards.Endpoints;

public class LeaderboardRequest {
	/// <summary>
	/// Id of leaderboard
	/// </summary>
	public required string Leaderboard { get; set; }
	
	/// <summary>
	/// Use new leaderboard backend (will be default in the future)
	/// </summary>
	[QueryParam, DefaultValue(true)]
	public bool? New { get; set; } = true;
}

internal sealed class LeaderboardRequestValidator : Validator<LeaderboardRequest> {
	public LeaderboardRequestValidator() {
		var lbSettings = Resolve<IOptions<ConfigLeaderboardSettings>>();
		var newLbService = Resolve<ILeaderboardRegistrationService>();
		RuleFor(x => x.Leaderboard)
			.NotEmpty()
			.WithMessage("Leaderboard is required")
			.When(x => lbSettings.Value.HasLeaderboard(x.Leaderboard) 
			           || (x.New is true && newLbService.LeaderboardsById.ContainsKey(x.Leaderboard)))
			.WithMessage("Leaderboard does not exist");
	}
}