using EliteAPI.Authentication;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Accounts.RefreshPurchases;

internal sealed class UnlinkAccountEndpoint(
	IMonetizationService monetizationService
) : Endpoint<DiscordIdRequest> {
	
	public override void Configure() {
		Post("/bot/account/{DiscordId}/purchases");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Refresh User Purchases";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		await monetizationService.FetchUserEntitlementsAsync(request.DiscordIdUlong);
		await SendOkAsync(cancellation: c);
	}
}