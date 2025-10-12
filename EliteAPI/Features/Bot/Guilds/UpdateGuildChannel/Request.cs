using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Incoming;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildChannel;

public class BotUpdateGuildChannelRequest : DiscordIdRequest {
	[FromBody] public required IncomingGuildChannelDto Channel { get; set; }
}

internal sealed class BotUpdateGuildChannelRequestValidator : Validator<BotUpdateGuildChannelRequest> {
	public BotUpdateGuildChannelRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}