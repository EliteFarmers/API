using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Incoming;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildRole;

public class BotUpdateGuildRoleRequest : DiscordIdRequest {
	[FromBody] public required IncomingGuildRoleDto Role { get; set; }
}

internal sealed class BotUpdateGuildRoleRequestValidator : Validator<BotUpdateGuildRoleRequest> {
	public BotUpdateGuildRoleRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}