using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Images.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public class ProductImage {
	[ForeignKey("Product")] public ulong ProductId { get; set; }
	public Product Product { get; set; } = null!;

	[ForeignKey("Image")] public required string ImageId { get; set; }
	public Image Image { get; set; } = null!;
}

public class ProductImageEntityConfiguration : IEntityTypeConfiguration<ProductImage> {
	public void Configure(EntityTypeBuilder<ProductImage> builder) {
		builder.HasKey(pi => new { pi.ProductId, pi.ImageId });
	}
}