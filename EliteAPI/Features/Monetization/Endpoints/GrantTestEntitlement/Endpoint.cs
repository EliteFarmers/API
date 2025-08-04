using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Monetization.Endpoints.GrantTestEntitlement;

internal sealed class GrantTestEntitlementEndpoint(
	IMonetizationService monetizationService)
	: Endpoint<UserEntitlementRequest>
{
	public override void Configure() {
		Post("/account/{DiscordId}/entitlement/{ProductId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Description(s => s.Accepts<UserEntitlementRequest>());
		
		Summary(s => {
			s.Summary = "Grant a test entitlement to a user or guild";
			s.Description = "This passes along a request to Discord to grant a test entitlement to a user or guild, which only works on subscription products.";
		});
	}

	public override async Task HandleAsync(UserEntitlementRequest request, CancellationToken c) 
	{
		var result = await monetizationService.GrantTestEntitlementAsync(request.DiscordIdUlong, request.ProductIdUlong, request.Target ?? EntitlementTarget.User);
		
		if (result is StatusCodeResult statusCodeResult) {
			await Send.ResponseAsync(statusCodeResult, statusCodeResult.StatusCode, c);
			return;
		}

		await Send.NoContentAsync(cancellation: c);
	}
}