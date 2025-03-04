using EliteAPI.Authentication;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Badges.GrantBadge;

internal sealed class GrantBadgeEndpoint(
	IBadgeService badgeService
) : Endpoint<BotGrantBadgeRequest> {
	
	public override void Configure() {
		Post("/bot/badges/{Player}/{BadgeId}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => {
			s.Summary = "Grant Badge";
		});
	}

	public override async Task HandleAsync(BotGrantBadgeRequest request, CancellationToken c) {
		await badgeService.AddBadgeToUser(request.Player, request.BadgeId);
		await SendNoContentAsync(cancellation: c);
	}
}