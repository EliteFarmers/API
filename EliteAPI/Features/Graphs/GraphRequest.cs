using System.ComponentModel;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;
using Newtonsoft.Json;

namespace EliteAPI.Features.Graphs;

public class GraphRequest : PlayerProfileUuidRequest
{
	/// <summary>
	/// Unix timestamp in seconds for the start of the data to return
	/// </summary>
	[QueryParam]
	[DefaultValue(0)]
	public long? From { get; set; } = 0;

	/// <summary>
	/// Amount of days after the "from" timestamp to include
	/// </summary>
	[QueryParam]
	[DefaultValue(7)]
	public int? Days { get; set; } = 7;

	/// <summary>
	/// Data points returned per 24-hour period
	/// </summary>
	[QueryParam]
	[DefaultValue(4)]
	public int? PerDay { get; set; } = 4;

	[JsonIgnore]
	public DateTimeOffset Start => From == 0
		? DateTimeOffset.UtcNow.AddDays(-(Days ?? 7))
		: DateTimeOffset.FromUnixTimeSeconds(From ?? 0);

	[JsonIgnore]
	public DateTimeOffset End => From == 0
		? DateTimeOffset.UtcNow
		: DateTimeOffset.FromUnixTimeSeconds(From ?? 0) + TimeSpan.FromDays(Days ?? 7);
}

internal sealed class GraphRequestValidator : Validator<GraphRequest>
{
	public GraphRequestValidator() {
		Include(new PlayerProfileUuidRequestValidator());

		RuleFor(r => r.From)
			.GreaterThanOrEqualTo(0)
			.WithMessage("From must be greater than or equal to 0.");

		RuleFor(r => r.Days)
			.GreaterThan(0)
			.WithMessage("Days must be greater than 0.")
			.LessThanOrEqualTo(30)
			.WithMessage("Days must be less than 30.");

		RuleFor(r => r.PerDay)
			.InclusiveBetween(1, 4)
			.WithMessage("PerDay must be greater than 0 and less than 5.");

		RuleFor(r => r.Start)
			.LessThanOrEqualTo(r => r.End)
			.WithMessage("Evaluated start time cannot be greater than end time.");

		RuleFor(r => r.Start)
			.GreaterThanOrEqualTo(r => r.End.AddDays(-30.01))
			.WithMessage("Evaluated time range cannot be greater than 30 days.");
	}
}