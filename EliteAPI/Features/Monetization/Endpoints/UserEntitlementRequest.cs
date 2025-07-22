using System.Text.Json.Serialization;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Monetization.Endpoints;

public class UserEntitlementRequest : DiscordIdRequest 
{
	public long ProductId { get; set; }
	
	[JsonIgnore]
	public ulong ProductIdUlong => (ulong) ProductId;
	
	[QueryParam]
	public EntitlementTarget? Target { get; set; } = EntitlementTarget.User;	
}

internal sealed class UserEntitlementRequestValidator : Validator<UserEntitlementRequest> {
	public UserEntitlementRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.ProductId)
			.GreaterThan(0)
			.WithMessage("ProductId is required");
	}
}