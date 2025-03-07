using FastEndpoints;
using FluentValidation;
using Newtonsoft.Json;

namespace EliteAPI.Models.Common;

public class EventIdRequest {
	public required long EventId { get; set; }
	
	[JsonIgnore]
	public ulong EventIdUlong => (ulong) EventId;
}

internal sealed class EventIdRequestValidator : Validator<EventIdRequest> {
	public EventIdRequestValidator() {
		RuleFor(x => x.EventId)
			.NotEmpty()
			.WithMessage("EventId is required")
			.GreaterThan(10_000_000_000_000_000)
			.WithMessage("EventId must be at least 17 digits long and positive");
	}
}