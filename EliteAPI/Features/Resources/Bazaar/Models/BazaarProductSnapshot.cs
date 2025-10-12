using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Bazaar;

public class BazaarProductSnapshot {
	[Key] public long Id { get; set; }

	[Required] [MaxLength(128)] public string ProductId { get; set; } = string.Empty;

	public DateTimeOffset RecordedAt { get; set; }

	/// <summary>
	/// What the player receives when selling instantly (from API's quick_status.sellPrice)
	/// </summary>
	public double InstaSellPrice { get; set; }

	/// <summary>
	/// What the player pays when buying instantly (from API's quick_status.buyPrice)
	/// </summary>
	public double InstaBuyPrice { get; set; }

	/// <summary>
	/// Representative price for a new buy order (from sell_summary)
	/// </summary>
	public double BuyOrderPrice { get; set; }

	/// <summary>
	/// Representative price for a new sell order (from buy_summary)
	/// </summary>
	public double SellOrderPrice { get; set; }

	// /// <summary>
	// /// Amount of sold items averaged over the last week
	// /// </summary>
	// public long SellVolumeWeekly { get; set; }
	//
	// /// <summary>
	// /// Amount of bought items averaged over the last week
	// /// </summary>
	// public long BuyVolumeWeekly { get; set; }
}

public class BazaarProductSnapshotConfiguration : IEntityTypeConfiguration<BazaarProductSnapshot> {
	public void Configure(EntityTypeBuilder<BazaarProductSnapshot> builder) {
		builder.HasIndex(p => new { p.ProductId, p.RecordedAt }).IsUnique();
		builder.HasIndex(p => p.RecordedAt);
		builder.HasIndex(p => p.ProductId);
	}
}