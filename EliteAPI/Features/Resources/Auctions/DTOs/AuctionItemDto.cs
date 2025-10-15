using EliteAPI.Features.Resources.Auctions.Models;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Auctions.DTOs;

public class AuctionItemDto
{
	public required string SkyblockId { get; set; }
	public required string VariantKey { get; set; }

	/// <summary>
	/// Data used to generate variant key (easier to parse)
	/// </summary>
	public AuctionItemVariation VariedBy => AuctionItemVariation.FromKey(VariantKey);

	/// <summary>
	/// Lowest price seen recently (excluding outliers)
	/// </summary>
	public decimal Lowest { get; set; } = -1;

	/// <summary>
	/// Volume of prices used to get the lowest recent price
	/// </summary>
	public int LowestVolume { get; set; }

	/// <summary>
	/// Lowest price seen in 3 days (excluding outliers)
	/// </summary>
	public decimal Lowest3Day { get; set; } = -1;

	/// <summary>
	/// Volume of prices used to get the lowest 3 day price
	/// </summary>
	public int Lowest3DayVolume { get; set; }

	/// <summary>
	/// Lowest price seen in 7 days (excluding outliers)
	/// </summary>
	public decimal Lowest7Day { get; set; } = -1;

	/// <summary>
	/// Volume of prices ued to get lowest 3 day price
	/// </summary>
	public int Lowest7DayVolume { get; set; }
}

[Mapper]
public static partial class AuctionItemMapper
{
	[MapperIgnoreSource(nameof(AuctionItem.CalculatedAt))]
	[MapperIgnoreSource(nameof(AuctionItem.LowestObservedAt))]
	public static partial AuctionItemDto ToDto(this AuctionItem auctionItem);
}