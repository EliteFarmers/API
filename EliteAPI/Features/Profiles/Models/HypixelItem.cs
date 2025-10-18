using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Images.Models;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Profiles.Models;

public class HypixelItem
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long HypixelItemId { get; set; }

	public Guid? Uuid { get; set; }

	public required string SkyblockId { get; set; }

	public short Id { get; set; }
	public short Damage { get; set; } = 0;
	public short Count { get; set; } = 1;

	public string? Name { get; set; }

	public string? Lore { get; set; }

	public string? Modifier { get; set; }
	public string? RarityUpgrades { get; set; }
	public string? Timestamp { get; set; }
	public string? DonatedMuseum { get; set; }

	[Column(TypeName = "jsonb")] public Dictionary<string, int>? Enchantments { get; set; }
	[Column(TypeName = "jsonb")] public Dictionary<string, string>? Attributes { get; set; }
	[Column(TypeName = "jsonb")] public Dictionary<string, string?>? Gems { get; set; }

	public string? ImageId { get; set; }
	public Image? Image { get; set; }

	public string? Slot { get; set; }

	public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

	public long InventoryId { get; set; }
}

public class HypixelItemConfiguration : IEntityTypeConfiguration<HypixelItem>
{
	public void Configure(EntityTypeBuilder<HypixelItem> builder) {
		// Set the primary key
		builder.HasKey(item => item.HypixelItemId);

		builder.HasIndex(item => item.SkyblockId);

		builder.HasOne<Image>(item => item.Image)
			.WithMany()
			.HasForeignKey(item => item.ImageId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.Navigation(item => item.Image).AutoInclude();
	}
}