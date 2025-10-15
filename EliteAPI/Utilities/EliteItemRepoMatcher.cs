using EliteAPI.Models.DTOs.Outgoing;
using MinecraftRenderer;
using MinecraftRenderer.Nbt;
using SkyblockRepo;

namespace EliteAPI.Utilities;

public class EliteItemRepoMatcher : ISkyblockRepoMatcher
{
	public Type ItemType { get; } = typeof(ItemDto);

	public string? GetAttributeString(object item, string attribute) {
		if (item is not ItemDto dto || dto.Attributes is null) return null;
		return dto.Attributes.TryGetValue(attribute, out var value) ? value : null;
	}

	public string? GetSkyblockId(object item) {
		if (item is not ItemDto dto) return null;
		return dto.SkyblockId;
	}

	public string? GetName(object item) {
		if (item is not ItemDto dto) return null;
		return dto.Name;
	}
}

public class RenderContextRepoMatcher : ISkyblockRepoMatcher
{
	public Type ItemType { get; } = typeof(MinecraftBlockRenderer.SkullResolverContext);

	public string? GetAttributeString(object item, string attribute) {
		if (item is not MinecraftBlockRenderer.SkullResolverContext context) return null;
		if (context.ItemData?.CustomData?.TryGetValue(attribute, out var value) is true) {
			if (value is not NbtString stringTag) return null;
			return stringTag.Value;
		}

		return null;
	}

	public string? GetSkyblockId(object item) {
		if (item is not MinecraftBlockRenderer.SkullResolverContext context) return null;
		return context.CustomDataId;
	}

	public string? GetName(object item) {
		if (item is not MinecraftBlockRenderer.SkullResolverContext context) return null;
		return context.ItemId; // Not implemented in MinecraftRenderer
	}
}