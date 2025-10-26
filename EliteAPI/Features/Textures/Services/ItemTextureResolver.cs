using System.Text.RegularExpressions;
using EliteAPI.Data;
using EliteAPI.Features.Images.Models;
using EliteAPI.Features.Images.Services;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Features.Textures.Models;
using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using MinecraftRenderer;
using MinecraftRenderer.Hypixel;
using MinecraftRenderer.Nbt;
using SixLabors.ImageSharp;
using SkyblockRepo;
using SkyblockRepo.Models;

namespace EliteAPI.Features.Textures.Services;

[RegisterService<ItemTextureResolver>(LifeTime.Scoped)]
public partial class ItemTextureResolver(
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

	public NbtCompound GetItemNbt(string skyblockId, bool canBePet = false) {
		var item = SkyblockRepoClient.Data.Items.GetValueOrDefault(skyblockId);
		var mappedId = item?.Data?.Material is not null
			? LegacyItemMappings.MapBukkitIdOrDefault(item.Data.Material, (short)item.Data.Durability)
			: null;

		SkyblockPetData? pet = null;
		if (canBePet && (SkyblockRepoClient.Data.Pets.TryGetValue(skyblockId, out pet) || item is null)) {
			mappedId = LegacyItemMappings.MapNumericIdOrDefault(397, 3); // Player head
		}

		NbtCompound root;

		if (mappedId is null) {
			root = new NbtCompound(new Dictionary<string, NbtTag> {
				["id"] = new NbtString("minecraft:player_head")
			});
			
			// Reformat RUNE_SPECIAL_1 to SPECIAL_RUNE;1
			var runeMatch = RuneRegex().Match(skyblockId);
			if (runeMatch.Success) {
				var name = runeMatch.Groups["name"].Value;
				var tier = runeMatch.Groups["tier"].Value;
			
				if (SkyblockRepoClient.Data.NeuItems.TryGetValue($"{name.ToUpperInvariant()}_RUNE;{tier}", out var runeItem)) {
					var skin = SkyblockRepoRegexUtils.ExtractSkullTexture(runeItem.NbtTag)?.Value;
					if (skin is not null) {
						root = root.WithProfileComponent(skin);
					}
				}

				return root;
			}

			if (item?.Data?.Skin?.Value is null) {
				return root;
			}
			
			mappedId = LegacyItemMappings.MapNumericIdOrDefault(397, 3);
		}

		var components = new List<KeyValuePair<string, NbtTag>>() {
			new(
				"minecraft:custom_data",
				new NbtCompound([
					new KeyValuePair<string, NbtTag>("id", new NbtString(skyblockId)),
				])),
		};

		var dyeColor = item?.Data?.Color;
		var decimalColor = dyeColor != null
			? Convert.ToInt32(string.Join("", dyeColor.Split(',').Select(c => int.Parse(c).ToString("X2"))), 16)
			: (int?)null;

		if (decimalColor.HasValue) {
			components.Add(new KeyValuePair<string, NbtTag>("minecraft:dyed_color", new NbtInt(decimalColor.Value)));
		}

		root = new NbtCompound(new Dictionary<string, NbtTag> {
			["id"] = new NbtString(mappedId),
			["components"] = new NbtCompound(components),
		});

		if (pet is not null) {
			var skin = pet.Rarities.Values.FirstOrDefault(v => v.Skin?.Value is not null)?.Skin?.Value;
			if (skin is not null) {
				root = root.WithProfileComponent(skin);
			}
		}

		return root;
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

		if (item.Attributes?.TryGetValue("skin_texture", out var skinData) is true) {
			root = root.WithProfileComponent(skinData);
		}

		if (item.Attributes?.TryGetValue("petInfo", out var petInfo) is true) {
			var info = PetParser.ParsePetInfoOrDefault(petInfo);
			if (info is not null && SkyblockRepoClient.Data.Pets.TryGetValue(info.Type, out var pet)) {
				var skin = pet.Rarities.Values.FirstOrDefault(v => v.Skin?.Value is not null)?.Skin?.Value;
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

	public async Task<MinecraftBlockRenderer.ResourceIdResult> GetItemResource(HypixelItem item,
		List<string>? packIds = null, int size = 64) {
		var root = GetItemNbt(item);
		var renderer = await provider.GetRendererAsync();
		var renderOptions = GetRenderOptions(renderer, packIds, size);
		return renderer.ComputeResourceIdFromNbt(root, renderOptions);
	}

	public async Task<string> RenderItemAndGetPathAsync(HypixelItem item, List<string>? packIds = null, int size = 64) {
		var root = GetItemNbt(item);
		return await RenderItemAndGetPathAsync(root, packIds, size);
	}

	public async Task<string> RenderItemAndGetPathAsync(string itemId, List<string>? packIds = null, int size = 64) {
		var root = GetItemNbt(itemId);
		return await RenderItemAndGetPathAsync(root, packIds, size);
	}

	public async Task<string> RenderPetAndGetPathAsync(string petId, List<string>? packIds = null, int size = 64) {
		var root = GetItemNbt(petId, true);
		return await RenderItemAndGetPathAsync(root, packIds, size);
	}

	public async Task<string>
		RenderItemAndGetPathAsync(NbtCompound root, List<string>? packIds = null, int size = 64) {
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
			}
			else {
				existingRenderedItem.LastUsed = DateTimeOffset.UtcNow;
				await context.SaveChangesAsync();

				return existingRenderedItem.ToUrl();
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

				return existingCheckRendered.ToUrl();
			}

			using var image = renderResult.CloneAsAnimatedImage();

			var savedImage = await imageService.ProcessAndUploadImageAsync(image,
				$"item-renders/{resourceId}", "item", token: c);

			var itemTexture = new HypixelItemTexture() {
				RenderHash = resourceId,
				Url = savedImage.Path
			};

			await context.HypixelItemTextures.AddAsync(itemTexture, c);
			await context.SaveChangesAsync(c);

			return savedImage.ToPrimaryUrl()!;
		});

		return result;
	}

	private MinecraftBlockRenderer.BlockRenderOptions GetRenderOptions(MinecraftBlockRenderer renderer,
		List<string>? packIds, int size) {
		if (packIds is null) {
			return provider.Options with { Size = size };
		}

		packIds = packIds.Where(p => p != "vanilla").ToList();

		if (packIds.Count == 0) {
			return provider.Options with { Size = size, PackIds = [] };
		}

		var validPacks = renderer.PackRegistry?.GetRegisteredPacks().Select(p => p.Id).ToList();
		if (validPacks is null || validPacks.Count == 0 || !packIds.All(p => validPacks.Contains(p))) {
			return provider.Options with { Size = size };
		}

		return provider.Options with { Size = size, PackIds = packIds };
	}
	
	[GeneratedRegex(@"^RUNE_(?<name>\w+)_(?<tier>\d)$")]
	public static partial Regex RuneRegex();
}