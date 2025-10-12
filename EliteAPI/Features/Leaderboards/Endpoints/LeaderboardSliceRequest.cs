using System.ComponentModel;
using System.Text.Json.Serialization;
using EliteAPI.Configuration.Settings;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards.Endpoints;

public class LeaderboardSliceRequest : LeaderboardRequest {
	[QueryParam] [DefaultValue(0)] public int? Offset { get; set; } = 0;

	[JsonIgnore] public int OffsetFormatted => Offset ?? 0;

	[QueryParam] [DefaultValue(20)] public int? Limit { get; set; } = 20;

	[JsonIgnore] public int LimitFormatted => Limit ?? 20;
}

internal sealed class LeaderboardSliceRequestValidator : Validator<LeaderboardSliceRequest> {
	public LeaderboardSliceRequestValidator() {
		Include(new LeaderboardRequestValidator());

		RuleFor(x => x.Offset)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Offset must be greater than or equal to 0");

		RuleFor(x => x.Limit)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Limit must be greater than or equal to 0")
			.LessThanOrEqualTo(10_000)
			.WithMessage("Limit must be less than or equal to 10,000");
	}
}