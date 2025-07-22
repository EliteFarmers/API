using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Monetization.Endpoints.RemoveTestEntitlement;

internal sealed class RemoveTestEntitlementEndpoint(
	IMonetizationService monetizationService)
	: Endpoint<UserEntitlementRequest>
{
	public override void Configure() {
		Delete("/account/{DiscordId}/entitlement/{ProductId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Remove a test entitlement from a user or guild";
			s.Description = "This passes along a request to Discord to remove a test entitlement from a user or guild.";
		});
	}

	public override async Task HandleAsync(UserEntitlementRequest request, CancellationToken c) 
	{
		var result = await monetizationService.RemoveTestEntitlementAsync(request.DiscordIdUlong, request.ProductIdUlong, request.Target ?? EntitlementTarget.User);
		
		if (result is StatusCodeResult statusCodeResult) {
			await SendAsync(statusCodeResult, statusCodeResult.StatusCode, c);
			return;
		}

		await SendNoContentAsync(cancellation: c);
	}
}