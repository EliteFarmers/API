using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Articles.Models;

public class PostTag
{
    [Key]
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? AccentColor { get; set; }
    
    public List<Article> Articles { get; set; } = [];
}

public class EntityTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.HasKey(t => t.Slug);

        builder.HasMany(t => t.Articles)
            .WithMany(a => a.Tags)
            .UsingEntity<ArticlePostTag>();
    }
}