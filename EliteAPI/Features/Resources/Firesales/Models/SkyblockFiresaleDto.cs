using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Firesales.Models;

[Mapper]
public static partial class SkyblockFiresaleMapper
{
	public static partial SkyblockFiresaleDto ToDto(this SkyblockFiresale firesale);
	public static partial IQueryable<SkyblockFiresaleDto> SelectDto(this IQueryable<SkyblockFiresale> firesales);

	[MapperIgnoreSource(nameof(SkyblockFiresaleItem.FiresaleId))]
	public static partial SkyblockFiresaleItemDto ToDto(this SkyblockFiresaleItem item);

	public static partial IQueryable<SkyblockFiresaleItemDto> SelectDto(this IQueryable<SkyblockFiresaleItem> items);
}

public class SkyblockFiresaleDto
{
	public long StartsAt { get; set; }
	public long EndsAt { get; set; }
	public List<SkyblockFiresaleItemDto> Items { get; set; } = [];
}

public class SkyblockFiresaleItemDto
{
	public required string ItemId { get; set; }
	public int SlotId { get; set; }
	public int Amount { get; set; }

	/// <summary>
	/// Price in Skyblock Gems
	/// </summary>
	public int Price { get; set; }

	/// <summary>
	/// Unix seconds
	/// </summary>
	public long StartsAt { get; set; }

	/// <summary>
	/// Unix seconds
	/// </summary>
	public long EndsAt { get; set; }
}