using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Guides.Models;

/// <summary>
/// Represents a user's bookmark/favorite of a guide.
/// </summary>
public class GuideBookmark
{
    public ulong UserId { get; set; }
    public int GuideId { get; set; }
    
    public Guide Guide { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GuideBookmarkConfiguration : IEntityTypeConfiguration<GuideBookmark>
{
    public void Configure(EntityTypeBuilder<GuideBookmark> builder)
    {
        builder.HasKey(b => new { b.UserId, b.GuideId });
        
        builder.HasOne(b => b.Guide)
            .WithMany()
            .HasForeignKey(b => b.GuideId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
