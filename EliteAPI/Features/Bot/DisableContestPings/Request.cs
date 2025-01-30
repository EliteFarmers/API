using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Bot.DisableContestPings;

public class DisableContestPingsRequest : DiscordIdRequest {
	[QueryParam]
	public string? Reason { get; set; }
}

internal sealed class DisableContestPingsRequestValidator : Validator<DisableContestPingsRequest> {
	public DisableContestPingsRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.Reason)
			.MaximumLength(128)
			.WithMessage("Reason must be 128 characters or less.")
			.When(x => !string.IsNullOrWhiteSpace(x.Reason));
	}
}