using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Auctions.Models;

public class AuctionBinPrice {
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }

	[Required] public required string SkyblockId { get; set; }

	[Required] public required string VariantKey { get; set; }

	public decimal Price { get; set; }

	public long ListedAt { get; set; }

	[Required] public required Guid AuctionUuid { get; set; }

	public DateTimeOffset IngestedAt { get; set; }
}

public class AuctionBinPriceConfiguration : IEntityTypeConfiguration<AuctionBinPrice> {
	public void Configure(EntityTypeBuilder<AuctionBinPrice> builder) {
		builder.HasIndex(e => new { e.SkyblockId, e.VariantKey, ListedAtUnixMillis = e.ListedAt });
		builder.HasIndex(e => e.AuctionUuid);
		builder.HasIndex(e => e.IngestedAt);
	}
}