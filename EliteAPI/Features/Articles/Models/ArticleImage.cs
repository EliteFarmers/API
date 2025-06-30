using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Articles.Models;

public class ArticleImage {
    [ForeignKey(nameof(Article))]
    public long ArticleId { get; set; }
    public Article Article { get; set; } = null!;
	
    [ForeignKey(nameof(Image))]
    public required string ImageId { get; set; }
    public Image Image { get; set; } = null!;
}

public class ArticleImageEntityConfiguration : IEntityTypeConfiguration<ArticleImage>
{
    public void Configure(EntityTypeBuilder<ArticleImage> builder)
    {
        builder.HasKey(pi => new { pi.ArticleId, pi.ImageId });
    }
}