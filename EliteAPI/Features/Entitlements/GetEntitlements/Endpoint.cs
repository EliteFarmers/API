using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Entitlements.GetEntitlements;

internal sealed class GetEntitlementsEndpoint(
	IMonetizationService monetizationService,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEntitlementsRequest, List<EntitlementDto>>
{
	public override void Configure() {
		Get("/account/{DiscordId}/entitlements");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Get all entitlements for a user or guild";
		});
	}

	public override async Task HandleAsync(GetEntitlementsRequest request, CancellationToken c) 
	{
		var entitlements = request.Target == EntitlementTarget.User 
			?  mapper.Map<List<EntitlementDto>>(await monetizationService.GetUserEntitlementsAsync(request.DiscordIdUlong))
			:  mapper.Map<List<EntitlementDto>>(await monetizationService.GetGuildEntitlementsAsync(request.DiscordIdUlong));

		await SendAsync(entitlements, cancellation: c);
	}
}