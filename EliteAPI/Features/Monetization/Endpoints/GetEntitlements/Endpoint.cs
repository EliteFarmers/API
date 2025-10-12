using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Monetization.Endpoints.GetEntitlements;

internal sealed class GetEntitlementsEndpoint(
	IMonetizationService monetizationService,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEntitlementsRequest, List<EntitlementDto>> {
	public override void Configure() {
		Get("/account/{DiscordId}/entitlements");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(x => x.Accepts<GetEntitlementsRequest>());

		Summary(s => { s.Summary = "Get all entitlements for a user or guild"; });
	}

	public override async Task HandleAsync(GetEntitlementsRequest request, CancellationToken c) {
		var entitlements =
			mapper.Map<List<EntitlementDto>>(await monetizationService.GetEntitlementsAsync(request.DiscordIdUlong));

		await Send.OkAsync(entitlements, c);
	}
}