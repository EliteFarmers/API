using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;

namespace EliteAPI.Features.Entitlements.GetEntitlements;

public class GetEntitlementsRequest : DiscordIdRequest 
{
	[QueryParam]
	public EntitlementTarget? Target { get; set; } = EntitlementTarget.User;	
}

internal sealed class GetEntitlementsRequestValidator : Validator<GetEntitlementsRequest> {
	public GetEntitlementsRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}