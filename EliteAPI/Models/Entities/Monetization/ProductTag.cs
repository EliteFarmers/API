using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public class ProductTag {
	[ForeignKey(nameof(Product))]
	public ulong ProductId { get; set; }
	public Product Product { get; set; } = null!;

	[ForeignKey(nameof(Tag))]
	public int TagId { get; set; }
	public Tag Tag { get; set; } = null!;
	
	public int Order { get; set; }
}

public class ProductTagEntityConfiguration : IEntityTypeConfiguration<ProductTag>
{
	public void Configure(EntityTypeBuilder<ProductTag> builder)
	{
		builder.HasKey(pi => new { pi.ProductId, pi.TagId });
		builder.HasIndex(pi => pi.Order);
	}
}
