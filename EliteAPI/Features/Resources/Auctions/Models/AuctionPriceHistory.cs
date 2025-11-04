using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Auctions.Models;

public class AuctionPriceHistory
{
    [Key]
    public long Id { get; set; }

    [MaxLength(256)]
    public required string SkyblockId { get; set; }

    [MaxLength(512)]
    public string VariantKey { get; set; } = string.Empty;

    public long BucketStart { get; set; }
    public decimal? LowestBinPrice { get; set; }
    public decimal? AverageBinPrice { get; set; }
    public int BinListings { get; set; }
    public decimal? LowestSalePrice { get; set; }
    public decimal? AverageSalePrice { get; set; }
    public int SaleAuctions { get; set; }
    public int ItemsSold { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
}

public class AuctionPriceHistoryConfiguration : IEntityTypeConfiguration<AuctionPriceHistory>
{
    public void Configure(EntityTypeBuilder<AuctionPriceHistory> builder) {
        builder.Property(e => e.VariantKey).HasDefaultValue(string.Empty);
        builder.HasIndex(e => e.BucketStart);
        builder.HasIndex(e => new { e.SkyblockId, e.VariantKey, e.BucketStart }).IsUnique();
    }
}
