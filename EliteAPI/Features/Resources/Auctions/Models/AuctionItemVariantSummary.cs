using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Auctions.Models;

public class AuctionItemVariantSummary
{
    [Key]
    [Column(Order = 0)]
    public required string SkyblockId { get; set; }

    [Key]
    [Column(Order = 1)]
    public required string VariantKey { get; set; }

    public decimal? RecentLowestPrice { get; set; }
    public int RecentLowestPriceVolume { get; set; }
    public DateTimeOffset? RecentLowestPriceObservationTime { get; set; }

    public decimal? RepresentativeLowestPrice3Day { get; set; }
    public int RepresentativeLowestPrice3DayVolume { get; set; }

    public decimal? RepresentativeLowestPrice7Day { get; set; }
    public int RepresentativeLowestPrice7DayVolume { get; set; }

    public DateTimeOffset LastCalculatedUtc { get; set; }
}

public class AuctionItemVariantSummaryConfiguration : IEntityTypeConfiguration<AuctionItemVariantSummary>
{
    public void Configure(EntityTypeBuilder<AuctionItemVariantSummary> builder)
    {
        builder.HasKey(e => new { e.SkyblockId, e.VariantKey });
        builder.HasIndex(e => e.LastCalculatedUtc);
    }
}