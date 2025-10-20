using EliteAPI.Data;
using EliteAPI.Features.Images.Models;
using EliteAPI.Features.Images.Services;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using MinecraftRenderer;
using MinecraftRenderer.Hypixel;
using MinecraftRenderer.Nbt;
using SixLabors.ImageSharp;
using SkyblockRepo;

namespace EliteAPI.Features.Textures.Services;

[RegisterService<ItemTextureResolver>(LifeTime.Scoped)]
public class ItemTextureResolver(
	MinecraftRendererProvider provider,
	ILogger<ItemTextureResolver> logger,
	IImageService imageService,
	DataContext context,
	HybridCache cache,
	ISkyblockRepoClient repoClient)
{
	public async Task<byte[]?> GetPackIcon(string packId) {
		try {
			var renderer = await provider.GetRendererAsync();
			var image = renderer.GetTexturePackIcon(packId);

			if (image is null) {
				return null;
			}

			using var ms = new MemoryStream();
			await image.SaveAsPngAsync(ms);
			return ms.ToArray();
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to get pack icon for {PackId}", packId);
			throw;
		}
	}

	public async Task<byte[]> RenderItemAsync(string itemId, int size = 128) {
		try {
			var renderer = await provider.GetRendererAsync();
			using var image = renderer.RenderGuiItemFromTextureId(itemId, provider.Options with { Size = size });

			using var ms = new MemoryStream();
			await image.SaveAsPngAsync(ms);
			return ms.ToArray();
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to render item {ItemId}", itemId);
			throw;
		}
	}

	public NbtCompound GetItemNbt(HypixelItem item) {
		var extraAttributes =
			item.Attributes?.Select(a => new KeyValuePair<string, NbtTag>(a.Key, new NbtString(a.Value))) ?? [];
		var itemId = LegacyItemMappings.MapNumericIdOrDefault(item.Id, item.Damage);

		// Color is 3 RGB numbers with commas, e.g. "255,0,0"
		// We convert to decimal integer for Minecraft NBT
		var dyeColor = repoClient.FindItem(item.SkyblockId)?.Data?.Color;
		var decimalColor = dyeColor != null
			? Convert.ToInt32(string.Join("", dyeColor.Split(',').Select(c => int.Parse(c).ToString("X2"))), 16)
			: (int?)null;

		var components = new List<KeyValuePair<string, NbtTag>>() {
			new(
				"minecraft:custom_data",
				new NbtCompound([
					new KeyValuePair<string, NbtTag>("id", new NbtString(item.SkyblockId)),
					..extraAttributes
				])),
		};

		if (decimalColor.HasValue) {
			components.Add(new KeyValuePair<string, NbtTag>("minecraft:dyed_color", new NbtInt(decimalColor.Value)));
		}

		var root = new NbtCompound(new Dictionary<string, NbtTag> {
			["id"] = new NbtString(itemId),
			["count"] = new NbtByte((sbyte)item.Count),
			["components"] = new NbtCompound(components),
		});

		if (item.Attributes?.TryGetValue("petInfo", out var petInfo) is true) {
			var info = PetParser.ParsePetInfoOrDefault(petInfo);
			if (info is not null && SkyblockRepoClient.Data.Pets.TryGetValue(info.Type, out var pet)) {
				var skin = pet.Rarities.Values.FirstOrDefault()?.Skin?.Value;
				if (skin is not null) {
					root = root.WithProfileComponent(skin);
				}
			}
		}

		return root;
	}
	
	public async Task<string> GetItemResourceId(HypixelItem item, List<string>? packIds = null, int size = 64) {
		return (await GetItemResource(item, packIds, size)).ResourceId;
	}
	
	public async Task<MinecraftBlockRenderer.ResourceIdResult> GetItemResource(HypixelItem item, List<string>? packIds = null, int size = 64) {
		var root = GetItemNbt(item);
		var renderer = await provider.GetRendererAsync();
		var renderOptions = GetRenderOptions(renderer, packIds, size);
		return renderer.ComputeResourceIdFromNbt(root, renderOptions);
	}

	public async Task<string> RenderItemAndGetPathAsync(HypixelItem item, List<string>? packIds = null, int size = 64) {
		var root = GetItemNbt(item);

		var renderer = await provider.GetRendererAsync();
		var renderOptions = GetRenderOptions(renderer, packIds, size);
		var preResourceId = renderer.ComputeResourceIdFromNbt(root, renderOptions);

		var existingRenderedItem = await context.HypixelItemTextures
			.FirstOrDefaultAsync(i => i.RenderHash == preResourceId.ResourceId);

		if (existingRenderedItem is not null) {
			if (existingRenderedItem.LastUsed < DateTimeOffset.UtcNow.AddDays(-30)) {
				await imageService.DeleteImageAtPathAsync(existingRenderedItem.Url);
				context.HypixelItemTextures.Remove(existingRenderedItem);
				await context.SaveChangesAsync();
			} else {
				existingRenderedItem.LastUsed = DateTimeOffset.UtcNow;
				await context.SaveChangesAsync();

				return existingRenderedItem.Url;
			}
		}

		var result = await cache.GetOrCreateAsync(preResourceId.ResourceId, async c => {
			using var renderResult = renderer.RenderAnimatedItemFromNbtWithResourceId(root, renderOptions);
			var resourceId = renderResult.ResourceId.ResourceId;

			var existingCheckRendered =
				await context.HypixelItemTextures.FirstOrDefaultAsync(i => i.RenderHash == resourceId,
					cancellationToken: c);
			
			if (existingCheckRendered is not null) {
				existingCheckRendered.LastUsed = DateTimeOffset.UtcNow;
				await context.SaveChangesAsync(c);

				return resourceId;
			}

			using var image = renderResult.CloneAsAnimatedImage();
			var savedImage = await imageService.ProcessAndUploadImageAsync(image,
				$"item-renders/{resourceId}", "item", token: c);
			savedImage.Hash = resourceId;
			
			await context.Images.AddAsync(savedImage, c);
			await context.SaveChangesAsync(c);

			return savedImage.ToPrimaryUrl()!;
		});
		
		return result;
	}
	
	private MinecraftBlockRenderer.BlockRenderOptions GetRenderOptions(MinecraftBlockRenderer renderer, List<string>? packIds, int size) {
		if (packIds is null)
		{
			return provider.Options with { Size = size };
		}
		
		packIds = packIds.Where(p => p != "vanilla").ToList();
		
		if (packIds.Count == 0) {
			return provider.Options with { Size = size, PackIds = [] };
		}

		var validPacks = renderer.PackRegistry?.GetRegisteredPacks().Select(p => p.Id).ToList();
		if (validPacks is null || validPacks.Count == 0 || !packIds.All(p => validPacks.Contains(p)))
		{
			return provider.Options with { Size = size };
		}

		return provider.Options with { Size = size, PackIds = packIds };
	}
}