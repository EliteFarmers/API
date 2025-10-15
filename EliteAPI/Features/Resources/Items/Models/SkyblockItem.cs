using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Bazaar;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Items.Models;

public class SkyblockItem
{
	public SkyblockItem() { }

	[SetsRequiredMembers]
	public SkyblockItem(string itemId) {
		ItemId = itemId;
	}

	public required string ItemId { get; set; }
	public BazaarProductSummary? BazaarProductSummary { get; set; }
	public List<AuctionItem>? AuctionItems { get; set; }

	public double NpcSellPrice { get; set; }

	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	[Column(TypeName = "jsonb")]
	public ItemResponse? Data { get; set; }
}

public class SkyblockItemConfiguration : IEntityTypeConfiguration<SkyblockItem>
{
	public void Configure(EntityTypeBuilder<SkyblockItem> builder) {
		builder.HasKey(x => x.ItemId);

		builder.HasOne(skyblockItem => skyblockItem.BazaarProductSummary)
			.WithOne(summary => summary.SkyblockItem)
			.HasForeignKey<BazaarProductSummary>(bazaarSummary => bazaarSummary.ItemId);

		builder.HasMany(skyblockItem => skyblockItem.AuctionItems)
			.WithOne()
			.HasForeignKey(variant => variant.SkyblockId);
	}
}