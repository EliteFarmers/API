using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Badges.RemoveBadge;

public class BotRemoveBadgeRequest : PlayerRequest {
	public int BadgeId { get; set; }
}

internal sealed class BotRemoveBadgeRequestValidator : Validator<BotRemoveBadgeRequest> {
	public BotRemoveBadgeRequestValidator() {
		Include(new PlayerRequestValidator());
	}
}