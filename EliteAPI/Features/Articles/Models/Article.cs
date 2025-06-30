using EliteAPI.Models.Entities.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Articles.Models;

public class Article
{
    public long Id { get; set; }
    public required string Title { get; set; } = string.Empty;
    public required string Content { get; set; } = string.Empty;
    public Image? Thumbnail { get; set; } = null;
    public string? Excerpt { get; set; } = null;
    public bool Published { get; set; } = false;
    
    public List<PostTag> Tags { get; set; } = [];
    public List<Image> Images { get; set; } = [];
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; } = null;
    public DateTimeOffset? DeletedAt { get; set; } = null;
}

public class ArticleEntityConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.Navigation(p => p.Thumbnail).AutoInclude();
        builder.Navigation(p => p.Images).AutoInclude();
		
        builder
            .HasMany(e => e.Tags)
            .WithMany(e => e.Articles)
            .UsingEntity<ArticlePostTag>();

        builder
            .HasMany(e => e.Images)
            .WithMany()
            .UsingEntity<ArticleImage>();
    }
}