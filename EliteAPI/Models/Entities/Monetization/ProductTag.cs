using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public class ProductTag {
	[ForeignKey(nameof(Product))]
	public int ProductId { get; set; }
	public Product Product { get; set; } = null!;

	[ForeignKey(nameof(Category))]
	public int CategoryId { get; set; }
	public Category Category { get; set; } = null!;
	
	public int Order { get; set; }
}

public class ProductTagEntityConfiguration : IEntityTypeConfiguration<ProductTag>
{
	public void Configure(EntityTypeBuilder<ProductTag> builder)
	{
		builder.HasKey(pi => new { pi.ProductId, pi.CategoryId });
	}
}
