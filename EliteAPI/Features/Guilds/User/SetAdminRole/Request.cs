using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;
using Newtonsoft.Json;

namespace EliteAPI.Features.Guilds.User.SetAdminRole;

public class SetAdminRoleRequest : DiscordIdRequest {
	[FromBody]
	public required string RoleId { get; set; }
	
	[JsonIgnore]
	public ulong RoleIdUlong => ulong.Parse(RoleId);
}

internal sealed class SetAdminRoleRequestValidator : Validator<SetAdminRoleRequest> {
	public SetAdminRoleRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.RoleId)
			.NotNull()
			.WithMessage("RoleId is required")
			.MinimumLength(17)
			.WithMessage("RoleId must be at least 17 characters long");
	}
}