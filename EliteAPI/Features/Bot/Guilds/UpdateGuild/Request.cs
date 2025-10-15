using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Incoming;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuild;

public class BotUpdateGuildRequest : DiscordIdRequest
{
	[FromBody] public required IncomingGuildDto Guild { get; set; }
}

internal sealed class BotUpdateGuildRequestValidator : Validator<BotUpdateGuildRequest>
{
	public BotUpdateGuildRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}