using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Articles.Models;

public class ArticlePostTag
{
    [ForeignKey(nameof(Article))]
    public long ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    
    [ForeignKey(nameof(Tag))]
    public required string TagSlug { get; set; }
    public PostTag Tag { get; set; } = null!;
}

public class ArticlePostTagEntityConfiguration : IEntityTypeConfiguration<ArticlePostTag>
{
    public void Configure(EntityTypeBuilder<ArticlePostTag> builder)
    {
        builder.HasKey(pi => new { pi.ArticleId, pi.TagSlug });
    }
}