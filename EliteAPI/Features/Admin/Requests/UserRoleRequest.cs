using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Admin.Requests;

public class UserRoleRequest : DiscordIdRequest
{
	public required string Role { get; set; }
}

internal sealed class UserRoleRequestValidator : Validator<UserRoleRequest>
{
	public UserRoleRequestValidator() {
		Include(new DiscordIdRequestValidator());

		RuleFor(x => x.Role)
			.NotEmpty()
			.WithMessage("Role is required");
	}
}