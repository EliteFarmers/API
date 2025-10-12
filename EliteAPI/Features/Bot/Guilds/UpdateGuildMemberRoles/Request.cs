using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildMemberRoles;

public class BotUpdateGuildMemberRolesRequest : DiscordIdRequest {
	public required string UserId { get; set; }

	[FromBody] public required List<string> Roles { get; set; }
}

internal sealed class BotUpdateGuildRoleRequestValidator : Validator<BotUpdateGuildMemberRolesRequest> {
	public BotUpdateGuildRoleRequestValidator() {
		Include(new DiscordIdRequestValidator());

		RuleFor(x => x.UserId)
			.NotEmpty()
			.WithMessage("UserId is required.");

		RuleFor(x => x.Roles)
			.NotEmpty()
			.WithMessage("Roles is required.")
			.Must(x => x.TrueForAll(y => ulong.TryParse(y, out _)))
			.WithMessage("Roles must be a list of role ids.");
	}
}