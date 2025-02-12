using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Entitlements.GrantTestEntitlement;

internal sealed class GrantTestEntitlementEndpoint(
	IMonetizationService monetizationService)
	: Endpoint<UserEntitlementRequest>
{
	public override void Configure() {
		Post("/account/{DiscordId}/entitlement/{ProductId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Grant a test entitlement to a user or guild";
			s.Description = "This passes along a request to Discord to grant a test entitlement to a user or guild, which only works on subscription products.";
		});
	}

	public override async Task HandleAsync(UserEntitlementRequest request, CancellationToken c) 
	{
		var result = await monetizationService.GrantTestEntitlementAsync(request.DiscordIdUlong, request.ProductIdUlong, request.Target ?? EntitlementTarget.User);
		
		if (result is StatusCodeResult statusCodeResult) {
			await SendAsync(statusCodeResult, statusCodeResult.StatusCode, c);
			return;
		}

		await SendOkAsync(cancellation: c);
	}
}