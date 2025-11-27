using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Resources.Auctions.Services;
using EliteAPI.Features.Textures.Services;
using EliteAPI.Mappers;
using FastEndpoints;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Services;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetInventoryItemMetaRequest
{
	public Guid InventoryUuid { get; set; }
	public required string SlotId { get; set; }
	
	[QueryParam]
	public string? Packs { get; set; }
	
	/// <summary>
	/// Sub slot if nested inventory
	/// </summary>
	[QueryParam]
	public string? Sub { get; set; }
	
	[JsonIgnore]
	public List<string> PackList => string.IsNullOrWhiteSpace(Packs)
		? []
		: Packs.Split(',').Select(p => p.Trim()).ToList();
}

internal sealed class InventoryItemMetaResponse
{
	/// <summary>
	/// Texture Pack ID where the item texture is located
	/// </summary>
	public string? PackId { get; set; }
	
	/// <summary>
	/// Variant key for price lookups
	/// </summary>
	public string? VariantKey { get; set; }
	
	/// <summary>
	/// Networth calculation result for this item
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public NetworthResult? Networth { get; set; }
}

internal sealed class GetInventoryItemMetaEndpoint(
	ItemTextureResolver itemTextureResolver,
	VariantKeyGenerator variantKeyGenerator,
	NetworthService networthService,
	DataContext context,
	ILogger<GetInventoryItemMetaEndpoint> logger
) : Endpoint<GetInventoryItemMetaRequest, InventoryItemMetaResponse>
{
	public override void Configure() {
		Get("/textures/{InventoryUuid}/{SlotId}/meta");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Inventory Item Texture Metadata"; });

		Options(o => { o.DisableRateLimiting(); });
	}

	public override async Task HandleAsync(GetInventoryItemMetaRequest request, CancellationToken c) {
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
		
		if (request.Sub is not null && itemData.Attributes?.Inventory is not null) {
			if (!itemData.Attributes.Inventory.TryGetValue(request.Sub, out var inventoryItem) || inventoryItem is null) {
				await Send.NotFoundAsync(c);
				return;
			}

			itemData = inventoryItem.ToHypixelItem();
		}

		var resource = await itemTextureResolver.GetItemResource(itemData, request.PackList);
		var itemDto = itemData.ToDto();
		
		// Calculate networth
		NetworthResult? networthResult = null;
		try
		{
			var networthItem = itemDto.ToNetworthItem();
			networthResult = await networthService.GetItemNetworthAsync(networthItem);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Failed to calculate networth for item {SlotId}", request.SlotId);
		}

		await Send.OkAsync(new InventoryItemMetaResponse
		{
			PackId = resource.SourcePackId,
			VariantKey = variantKeyGenerator.Generate(itemDto)?.ToKey(),
			Networth = networthResult
		}, cancellation: c);
	}
}