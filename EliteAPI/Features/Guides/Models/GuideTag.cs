using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Guides.Models;

public class GuideTag
{
    [Key]
    public int Id { get; set; }

    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(32)]
    public required string Category { get; set; }

    [MaxLength(7)]
    public string HexColor { get; set; } = "#FFFFFF";
    
    public List<GuideTagMapping> Guides { get; set; } = [];
}

public class GuideTagMapping
{
    public int GuideId { get; set; }
    public Guide Guide { get; set; } = null!;

    public int TagId { get; set; }
    public GuideTag Tag { get; set; } = null!;
}

public class GuideTagConfiguration : IEntityTypeConfiguration<GuideTag>
{
    public void Configure(EntityTypeBuilder<GuideTag> builder) 
    {
        builder.HasMany(t => t.Guides)
            .WithOne(g => g.Tag)
            .HasForeignKey(g => g.TagId);
    }
}

public class GuideTagMappingConfiguration : IEntityTypeConfiguration<GuideTagMapping>
{
    public void Configure(EntityTypeBuilder<GuideTagMapping> builder) 
    {
        builder.HasKey(t => new { t.GuideId, t.TagId });
    }
}
