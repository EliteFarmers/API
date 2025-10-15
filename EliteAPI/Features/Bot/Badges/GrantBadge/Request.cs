using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Badges.GrantBadge;

public class BotGrantBadgeRequest : PlayerRequest
{
	public int BadgeId { get; set; }
}

internal sealed class BotGrantBadgeRequestValidator : Validator<BotGrantBadgeRequest>
{
	public BotGrantBadgeRequestValidator() {
		Include(new PlayerRequestValidator());
	}
}