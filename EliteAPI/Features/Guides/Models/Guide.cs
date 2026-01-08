using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Guides.Models;

public class Guide
{
    [Key]
    public int Id { get; set; }

    public required ulong AuthorId { get; set; }
    public EliteAccount Author { get; set; } = null!;

    [MaxLength(256)]
    public string? Slug { get; set; }

    public GuideType Type { get; set; }

    public int? ActiveVersionId { get; set; }
    public GuideVersion? ActiveVersion { get; set; }

    public int? DraftVersionId { get; set; }
    public GuideVersion? DraftVersion { get; set; }

    public GuideStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<GuideVersion> Versions { get; set; } = [];
    public List<GuideTagMapping> Tags { get; set; } = [];
    public List<GuideVote> Votes { get; set; } = [];
    
    public int Score { get; set; } = 0;
    
    /// <summary>
    /// View count, only incremented for authenticated users.
    /// </summary>
    public int ViewCount { get; set; } = 0;
    
    /// <summary>
    /// Reason provided by moderator when rejecting the guide.
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// Soft delete flag for author-initiated deletions.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}

public enum GuideType
{
    General,
    Farm,
    Greenhouse,
    Contest
}

public enum GuideStatus
{
    Draft,
    PendingApproval,
    Published,
    Rejected,
    Archived
}

public class GuideConfiguration : IEntityTypeConfiguration<Guide>
{
    public void Configure(EntityTypeBuilder<Guide> builder) 
    {
        builder.HasOne(g => g.ActiveVersion)
            .WithOne()
            .HasForeignKey<Guide>(g => g.ActiveVersionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(g => g.DraftVersion)
            .WithOne()
            .HasForeignKey<Guide>(g => g.DraftVersionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(g => g.Versions)
            .WithOne(v => v.Guide)
            .HasForeignKey(v => v.GuideId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(g => g.Tags)
            .WithOne(t => t.Guide)
            .HasForeignKey(t => t.GuideId);
    }
}
