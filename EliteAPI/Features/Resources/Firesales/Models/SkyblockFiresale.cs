using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Firesales.Models;

public class SkyblockFiresale
{
	[MapperIgnore] public Guid Id { get; set; } = Guid.CreateVersion7();
	public required long StartsAt { get; set; }
	public required long EndsAt { get; set; }

	public List<SkyblockFiresaleItem> Items { get; set; } = [];
}

public class SkyblockFiresaleItem
{
	public Guid FiresaleId { get; set; }
	public required string ItemId { get; set; }
	public required int Amount { get; set; }
	public required int Price { get; set; }
	public required long StartsAt { get; set; }
	public required long EndsAt { get; set; }
}

public class SkyblockFiresaleConfiguration : IEntityTypeConfiguration<SkyblockFiresale>
{
	public void Configure(EntityTypeBuilder<SkyblockFiresale> builder) {
		builder.HasKey(x => x.Id);

		builder.HasMany(x => x.Items)
			.WithOne()
			.HasForeignKey(x => x.FiresaleId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.StartsAt);
	}
}

public class SkyblockFiresaleItemConfiguration : IEntityTypeConfiguration<SkyblockFiresaleItem>
{
	public void Configure(EntityTypeBuilder<SkyblockFiresaleItem> builder) {
		builder.HasKey(x => new { x.FiresaleId, x.ItemId });
	}
}