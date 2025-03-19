using System.ComponentModel;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerRank;

public class GetPlayerRankRequest : PlayerProfileUuidRequest {
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
	/// Start at a specified rank for upcoming players
	/// </summary>
	[QueryParam]
	public int? AtRank { get; set; } = -1;
}

internal sealed class GetPlayerRankRequestValidator : Validator<GetPlayerRankRequest> {
	public GetPlayerRankRequestValidator() {
		Include(new PlayerProfileUuidRequestValidator());
		var lbSettings = Resolve<IOptions<ConfigLeaderboardSettings>>();
		RuleFor(x => x.Leaderboard)
			.NotEmpty()
			.WithMessage("Leaderboard is required")
			.Must(lbSettings.Value.HasLeaderboard)
			.WithMessage("Leaderboard does not exist");
	}
}