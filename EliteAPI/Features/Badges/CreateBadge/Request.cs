using EliteAPI.Features.Images.Models;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Badges.CreateBadge;

public class CreateBadgeRequest {
	[FromForm]
	public required CreateBadge Badge { get; set; }
	
	public class CreateBadge {
		public required string Name { get; set; }
		public required string Description { get; set; }
		public required string Requirements { get; set; }
		public bool TieToAccount { get; set; } = false;
		[AllowedFileExtensions]
		public IFormFile? Image { get; set; }
	}
}

internal sealed class UpdateBadgeRequestValidator : Validator<CreateBadgeRequest> {
	public UpdateBadgeRequestValidator() {
		RuleFor(x => x.Badge)
			.NotNull()
			.WithMessage("Badge must be provided");
		RuleFor(x => x.Badge.Name)
			.MaximumLength(50)
			.WithMessage("Name must be less than 50 characters");
		RuleFor(x => x.Badge.Description)
			.MaximumLength(1024)
			.WithMessage("Description must be less than 1024 characters");
		RuleFor(x => x.Badge.Requirements)
			.MaximumLength(512)
			.WithMessage("Requirements must be less than 512 characters");
	}
}