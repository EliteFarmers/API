using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Account.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Inventories;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Auctions.Models;

[Mapper]
public static partial class AuctionMapper
{
	public static partial AuctionDto ToDto(this Auction auction);

	public static ItemDto? ToDto(this byte[] itemBytes) {
		return NbtParser.NbtToItem(Convert.ToBase64String(itemBytes));	
	}
}

public class AuctionDto
{
	public Guid AuctionId { get; set; }
	
	public Guid SellerUuid { get; set; }
	public Guid SellerProfileUuid { get; set; }
	[MapperIgnore]
	public AccountMetaDto? Seller { get; set; }
	
	public Guid? BuyerUuid { get; set; }
	public Guid? BuyerProfileUuid { get; set; }
	[MapperIgnore]
	public AccountMetaDto? Buyer { get; set; }
	
	public long Start { get; set; }
	public long End { get; set; }
	public long SoldAt { get; set; }
	public long Price { get; set; }
	public short Count { get; set; }
	
	public bool Bin { get; set; }
	public string? SkyblockId { get; set; }
	public string VariantKey { get; set; } = string.Empty;
	public string? ItemUuid { get; set; }
	
	public ItemDto? Item { get; set; }
	public DateTimeOffset LastUpdatedAt { get; set; }
	public long StartingBid { get; set; }
	public long? HighestBid { get; set; }
}