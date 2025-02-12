using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Badges;

public class PlayerBadgeRequest : PlayerRequest {
	/// <summary>
	/// ID of the badge
	/// </summary>
	public int BadgeId { get; set; }
}

internal sealed class PlayerBadgeRequestValidator : Validator<PlayerBadgeRequest> {
	public PlayerBadgeRequestValidator() {
		Include(new PlayerRequestValidator());

		RuleFor(x => x.BadgeId)
			.NotEmpty()
			.WithMessage("BadgeId is required")
			.GreaterThan(0)
			.WithMessage("BadgeId must be greater than 0");
	}
}