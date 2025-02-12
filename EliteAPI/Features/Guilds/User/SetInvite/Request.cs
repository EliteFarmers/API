using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guilds.User.SetInvite;

public class SetInviteRequest : DiscordIdRequest {
	[FromBody]
	public required string Invite { get; set; }
}

internal sealed class SetInviteRequestValidator : Validator<SetInviteRequest> {
	public SetInviteRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.Invite)
			.NotNull()
			.WithMessage("Invite is required")
			.MaximumLength(64)
			.WithMessage("Invite must have a max length of 64 characters");
	}
}