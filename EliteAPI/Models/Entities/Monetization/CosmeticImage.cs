using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public class CosmeticImage {
	[ForeignKey("Cosmetic")]
	public int CosmeticId { get; set; }
	public WeightStyle Cosmetic { get; set; } = null!;
	
	[ForeignKey("Image")]
	public required string ImageId { get; set; }
	public Image Image { get; set; } = null!;
}

public class CosmeticImageEntityConfiguration : IEntityTypeConfiguration<CosmeticImage>
{
	public void Configure(EntityTypeBuilder<CosmeticImage> builder)
	{
		builder.HasKey(pi => new { pi.CosmeticId, pi.ImageId });
	}
}