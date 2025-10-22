using System.Text.Json.Serialization;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Events.User.JoinEvent;

internal sealed class JoinEventRequest
{
	[BindFrom("eventId")] public ulong EventId { get; set; }
	[QueryParam] public string? PlayerUuid { get; set; }
	[JsonIgnore] public string? PlayerUuidFormatted => PlayerUuid?.ToLowerInvariant().Replace("-", "");
	[QueryParam] public string? ProfileUuid { get; set; }
	[JsonIgnore] public string? ProfileUuidFormatted => ProfileUuid?.ToLowerInvariant().Replace("-", "");
}

internal sealed class JoinEventRequestValidator : Validator<JoinEventRequest>
{
	public JoinEventRequestValidator() {
		RuleFor(x => x.PlayerUuid)
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$")
			.When(x => x.PlayerUuid is not null);

		RuleFor(x => x.ProfileUuid)
			.NotEmpty()
			.WithMessage("PlayerUuid is required")
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$")
			.When(x => x.ProfileUuid is not null);
	}
}