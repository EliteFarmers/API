using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public class ProductCategory {
	[ForeignKey(nameof(Product))]
	public ulong ProductId { get; set; }
	public Product Product { get; set; } = null!;

	[ForeignKey(nameof(Category))]
	public int CategoryId { get; set; }
	public Category Category { get; set; } = null!;
	
	public int Order { get; set; }
}

public class ProductCategoryEntityConfiguration : IEntityTypeConfiguration<ProductCategory>
{
	public void Configure(EntityTypeBuilder<ProductCategory> builder)
	{
		builder.HasKey(pi => new { pi.ProductId, pi.CategoryId });
		builder.HasIndex(pi => pi.Order);
	}
}
