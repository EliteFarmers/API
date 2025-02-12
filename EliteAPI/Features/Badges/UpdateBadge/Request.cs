using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Badges.UpdateBadge;

public class UpdateBadgeRequest : BadgeRequest {
	public string? Name { get; set; }
	public string? Description { get; set; }
	public string? Requirements { get; set; }
	public IFormFile? Image { get; set; }
}

internal sealed class UpdateBadgeRequestValidator : Validator<UpdateBadgeRequest> {
	public UpdateBadgeRequestValidator() {
		Include(new BadgeRequestValidator());
		
		RuleFor(x => x.Name)
			.MaximumLength(50)
			.WithMessage("Name must be less than 50 characters")
			.When(x => x.Name is not null);
		RuleFor(x => x.Description)
			.MaximumLength(1024)
			.WithMessage("Description must be less than 1024 characters")
			.When(x => x.Description is not null);
		RuleFor(x => x.Requirements)
			.MaximumLength(512)
			.WithMessage("Requirements must be less than 512 characters")
			.When(x => x.Requirements is not null);
	}
}