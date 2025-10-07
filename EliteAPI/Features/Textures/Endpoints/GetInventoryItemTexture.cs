using System.Net.Mime;
using EliteAPI.Data;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetInventoryItemTextureRequest
{
	public Guid InventoryUuid { get; set; }
	public required string SlotId { get; set; }
}

internal sealed class GetInventoryItemTextureEndpoint(
	ItemTextureResolver itemTextureResolver,
	DataContext context
) : Endpoint<GetInventoryItemTextureRequest> {
	
	public override void Configure() {
		Get("/textures/{InventoryUuid}/{SlotId}");
		AllowAnonymous();
		Version(0);
		
		Summary(s => {
			s.Summary = "Get Inventory Item Texture";
		});
		
		Options(o => {
			o.DisableRateLimiting();
		});
	}
	
	public override async Task HandleAsync(GetInventoryItemTextureRequest request, CancellationToken c) {
		// Check if slotId has a file extension and remove it
		if (request.SlotId.Contains('.')) {
			request.SlotId = request.SlotId.Split('.')[0];
		}
		
		var inventory = await context.HypixelInventory
			.FirstOrDefaultAsync(i => i.HypixelInventoryId == request.InventoryUuid, cancellationToken: c);
		
		if (inventory is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var itemData = await context.HypixelItems
			.FirstOrDefaultAsync(i => i.Slot == request.SlotId && i.InventoryId == inventory.Id, cancellationToken: c);
		
		if (itemData is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var finalBytes = await itemTextureResolver.RenderItemAsync(itemData);
		
		await Send.BytesAsync(finalBytes, contentType: MediaTypeNames.Image.Png, cancellation: c);
	}
}