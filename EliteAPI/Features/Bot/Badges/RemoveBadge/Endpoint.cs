using EliteAPI.Authentication;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Badges.RemoveBadge;

internal sealed class RemoveBadgeEndpoint(
	IBadgeService badgeService
) : Endpoint<BotRemoveBadgeRequest> {
	
	public override void Configure() {
		Delete("/bot/badges/{Player}/{BadgeId}");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Remove Badge";
		});
	}

	public override async Task HandleAsync(BotRemoveBadgeRequest request, CancellationToken c) {
		await badgeService.RemoveBadgeFromUser(request.Player, request.BadgeId);
		await SendNoContentAsync(c);
	}
}