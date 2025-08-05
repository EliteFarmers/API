using EliteAPI.Authentication;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Accounts.RefreshPurchases;

internal sealed class RefreshUserPurchasesEndpoint(
	IMonetizationService monetizationService
) : Endpoint<DiscordIdRequest> {
	
	public override void Configure() {
		Post("/bot/account/{DiscordId}/purchases");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);
		
		Description(x => x.Accepts<DiscordIdRequest>());

		Summary(s => {
			s.Summary = "Refresh User Purchases";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		await monetizationService.FetchUserEntitlementsAsync(request.DiscordIdUlong);
		await Send.NoContentAsync(cancellation: c);
	}
}