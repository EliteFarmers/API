using System.ComponentModel.DataAnnotations;
using EliteAPI.Features.Resources.Items.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Bazaar;

public class BazaarProductSummary
{
	[Key] [Required] [MaxLength(100)] public required string ItemId { get; set; }
	public SkyblockItem SkyblockItem { get; set; } = null!;

	public DateTimeOffset CalculationTimestamp { get; set; }

	public double InstaSellPrice { get; set; }
	public double InstaBuyPrice { get; set; }
	public double BuyOrderPrice { get; set; }
	public double SellOrderPrice { get; set; }
	public double AvgInstaSellPrice { get; set; }
	public double AvgInstaBuyPrice { get; set; }
	public double AvgBuyOrderPrice { get; set; }
	public double AvgSellOrderPrice { get; set; }
}

public class BazaarProductSummaryConfiguration : IEntityTypeConfiguration<BazaarProductSummary>
{
	public void Configure(EntityTypeBuilder<BazaarProductSummary> builder) {
		builder.HasKey(p => p.ItemId);
		builder.HasIndex(p => p.CalculationTimestamp);
	}
}