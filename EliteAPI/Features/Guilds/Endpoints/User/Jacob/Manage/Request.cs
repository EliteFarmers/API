using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class JacobManageRequest : DiscordIdRequest;

internal sealed class JacobManageRequestValidator : Validator<JacobManageRequest>
{
	public JacobManageRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}
