using EliteAPI.Configuration.Settings;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards;

public class LeaderboardRequest {
	/// <summary>
	/// Id of leaderboard
	/// </summary>
	public required string Leaderboard { get; set; }
}

internal sealed class LeaderboardRequestValidator : Validator<LeaderboardRequest> {
	public LeaderboardRequestValidator() {
		var lbSettings = Resolve<IOptions<ConfigLeaderboardSettings>>();
		RuleFor(x => x.Leaderboard)
			.NotEmpty()
			.WithMessage("Leaderboard is required")
			.When(x => lbSettings.Value.HasLeaderboard(x.Leaderboard))
			.WithMessage("Leaderboard does not exist");
	}
}