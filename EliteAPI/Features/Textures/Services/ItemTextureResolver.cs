using EliteAPI.Features.Profiles.Models;
using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using MinecraftRenderer.Hypixel;
using MinecraftRenderer.Nbt;
using SixLabors.ImageSharp;
using SkyblockRepo;

namespace EliteAPI.Features.Textures.Services;

[RegisterService<ItemTextureResolver>(LifeTime.Singleton)]
public class ItemTextureResolver(
	MinecraftRendererProvider provider,
	ILogger<ItemTextureResolver> logger,
	ISkyblockRepoClient repoClient)
{
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

	public async Task<byte[]> RenderItemAsync(HypixelItem item, int size = 128) {
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

		var renderer = await provider.GetRendererAsync();
		using var image = renderer.RenderItemFromNbt(root, provider.Options with { Size = size });

		using var ms = new MemoryStream();
		await image.SaveAsWebpAsync(ms);
		return ms.ToArray();
	}
}