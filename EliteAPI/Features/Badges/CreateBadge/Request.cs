using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Badges.CreateBadge;

public class CreateBadgeRequest : BadgeRequest {
	public required string Name { get; set; }
	public required string Description { get; set; }
	public required string Requirements { get; set; }
	public bool TieToAccount { get; set; }
	public IFormFile? Image { get; set; }
}

internal sealed class UpdateBadgeRequestValidator : Validator<CreateBadgeRequest> {
	public UpdateBadgeRequestValidator() {
		Include(new BadgeRequestValidator());

		RuleFor(x => x.Name)
			.MaximumLength(50)
			.WithMessage("Name must be less than 50 characters");
		RuleFor(x => x.Description)
			.MaximumLength(1024)
			.WithMessage("Description must be less than 1024 characters");
		RuleFor(x => x.Requirements)
			.MaximumLength(512)
			.WithMessage("Requirements must be less than 512 characters");
	}
}