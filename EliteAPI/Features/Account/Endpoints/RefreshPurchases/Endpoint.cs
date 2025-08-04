using EliteAPI.Features.Monetization.Services;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Account.RefreshPurchases;

internal sealed class RefreshPurchasesEndpoint(
	IMonetizationService monetizationService
) : EndpointWithoutRequest {
	
	public override void Configure() {
		Post("/account/purchases");
		Version(0);
		
		Description(x => x.ClearDefaultAccepts());
		
		Summary(s => {
			s.Summary = "Refresh Purchases";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}

		await monetizationService.SyncDiscordEntitlementsAsync(id.Value, false);
		await monetizationService.FetchUserEntitlementsAsync(id.Value);

		await Send.NoContentAsync(cancellation: c);
	}
}