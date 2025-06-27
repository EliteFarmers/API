using EliteAPI.Data;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Products.ClaimProduct;

internal sealed class ClaimProductEndpoint(
	DataContext context,
	IMonetizationService monetizationService
) : Endpoint<DiscordIdRequest> {
	
	public override void Configure() {
		Post("/product/{DiscordId}/claim");
		Version(0);
		
		Description(x => x.Accepts<DiscordIdRequest>());

		Summary(s => {
			s.Summary = "Claim Free Shop Product";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var userId = User.GetDiscordId();
		if (userId is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}
		
		var product = await context.Products
			.Where(p => p.Id == request.DiscordIdUlong)
			.FirstOrDefaultAsync(cancellationToken: c);
		
		if (product is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}

		if (!product.Available) {
			ThrowError("Not available", StatusCodes.Status400BadRequest);
		}
		
		if (product.Price != 0) {
			ThrowError("Product is not free!", StatusCodes.Status400BadRequest);
		}

		if (product.Type != ProductType.Durable) {
			ThrowError("Product is not eligible to claim!", StatusCodes.Status400BadRequest);
		}
		
		await monetizationService.GrantProductAccessAsync(userId.Value, product.Id);
		
		await SendNoContentAsync(cancellation: c);
	}
}