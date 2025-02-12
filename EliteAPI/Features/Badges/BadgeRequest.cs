using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Badges;

public class BadgeRequest {
	/// <summary>
	/// ID of the badge
	/// </summary>
	public int BadgeId { get; set; }
}

internal sealed class BadgeRequestValidator : Validator<BadgeRequest> {
	public BadgeRequestValidator() {
		RuleFor(x => x.BadgeId)
			.NotEmpty()
			.WithMessage("BadgeId is required")
			.GreaterThan(0)
			.WithMessage("BadgeId must be greater than 0");
	}
}