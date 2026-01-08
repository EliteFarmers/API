using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Guides.Models;
using Microsoft.EntityFrameworkCore;
using Sqids;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Services;

[RegisterService<GuideService>(LifeTime.Scoped)]
public class GuideService(DataContext db)
{
    public async Task<Guide> CreateDraftAsync(ulong authorId, GuideType type)
    {
        var guide = new Guide
        {
            AuthorId = authorId,
            Type = type,
            Status = GuideStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        db.Guides.Add(guide);
        await db.SaveChangesAsync();

        // Generate initial slug based on ID
        guide.Slug = GetSlug(guide.Id);
        
        // Create initial empty draft version
        var initialVersion = new GuideVersion
        {
            GuideId = guide.Id,
            Title = "Untitled Guide",
            Description = "No description provided.",
            MarkdownContent = "# New Guide\nStart writing here...",
            CreatedAt = DateTime.UtcNow
        };
        
        db.GuideVersions.Add(initialVersion);
        await db.SaveChangesAsync();

        guide.DraftVersionId = initialVersion.Id;
        await db.SaveChangesAsync();

        return guide;
    }

    public async Task<Guide?> GetBySlugAsync(string slug)
    {
        return await db.Guides
            .Include(g => g.Author)
                .ThenInclude(a => a.MinecraftAccounts)
            .Include(g => g.Author)
                .ThenInclude(a => a.UserSettings)
            .Include(g => g.ActiveVersion)
            .Include(g => g.DraftVersion)
            .Include(g => g.Tags)
                .ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(g => g.Slug == slug);
    }

    public async Task<Guide?> GetByIdAsync(int id)
    {
        return await db.Guides
            .Include(g => g.Author)
            .Include(g => g.ActiveVersion)
            .Include(g => g.DraftVersion)
            .FirstOrDefaultAsync(g => g.Id == id);
    }
    
    public string GetSlug(int id)
    {
        return EliteAPI.Features.Common.Services.SqidService.Encode(id);
    }

    public int? GetIdFromSlug(string slug)
    {
        return EliteAPI.Features.Common.Services.SqidService.Decode(slug);
    }

    public async Task UpdateDraftAsync(int guideId, string title, string description, string markdown, GuideRichData? richData)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null) throw new KeyNotFoundException("Guide not found");

        var draft = await db.GuideVersions.FindAsync(guide.DraftVersionId);
        if (draft == null)
        {
            // Should not happen if data integrity is kept, but let's handle it
            draft = new GuideVersion
            {
                GuideId = guide.Id,
                Title = title,
                Description = description,
                MarkdownContent = markdown,
                RichBlocks = richData,
                CreatedAt = DateTime.UtcNow
            };
            db.GuideVersions.Add(draft);
            await db.SaveChangesAsync();
            
            guide.DraftVersionId = draft.Id;
        }
        else
        {
            draft.Title = title;
            draft.Description = description;
            draft.MarkdownContent = markdown;
            draft.RichBlocks = richData;
            // We don't update CreatedAt, effectively Draft is a single mutable version until published? 
            // Or should we create a NEW version on every save? 
            // Usually "Draft" is mutable. "Published" is immutable/snapshot.
        }
        
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task SubmitForApprovalAsync(int guideId)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null) return;
        
        guide.Status = GuideStatus.PendingApproval;
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task PublishAsync(int guideId, ulong adminId)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null) return;
        
        // When publishing, the Draft becomes the Active version.
        if (guide.DraftVersionId == null) return;

        var draft = await db.GuideVersions.FindAsync(guide.DraftVersionId);
        if (draft == null) return;

        // Create a copy for the published version
        var publishedVersion = new GuideVersion
        {
            GuideId = guide.Id,
            Title = draft.Title,
            Description = draft.Description,
            MarkdownContent = draft.MarkdownContent,
            RichBlocks = draft.RichBlocks,
            CreatedAt = DateTime.UtcNow,
            IconItemName = draft.IconItemName
        };

        db.GuideVersions.Add(publishedVersion);
        await db.SaveChangesAsync();

        guide.ActiveVersionId = publishedVersion.Id;
        guide.Status = GuideStatus.Published;
        guide.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync();
    }

    public async Task<bool> RejectGuideAsync(int guideId, ulong adminId)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null) return false;
        
        guide.Status = GuideStatus.Rejected;
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task VoteGuideAsync(int guideId, ulong userId, short value)
    {
        if (Math.Abs(value) != 1) throw new ArgumentException("Vote value must be 1 or -1");

        var guideExists = await db.Guides.AnyAsync(g => g.Id == guideId);
        if (!guideExists) throw new KeyNotFoundException("Guide not found.");

        var vote = await db.GuideVotes.FindAsync(guideId, userId);
        if (vote != null)
        {
            vote.Value = value;
            vote.VotedAt = DateTime.UtcNow;
        }
        else
        {
            vote = new GuideVote
            {
                GuideId = guideId,
                UserId = userId,
                Value = value
            };
            db.GuideVotes.Add(vote);
        }

        await db.SaveChangesAsync();

        // Update aggregate score
        var score = await db.GuideVotes.Where(v => v.GuideId == guideId).SumAsync(v => v.Value);
        
        var guide = await db.Guides.FindAsync(guideId);
        if (guide != null)
        {
            guide.Score = score;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Returns count of user's non-deleted guides for slot management.
    /// </summary>
    public async Task<int> GetAuthorGuideCountAsync(ulong authorId)
    {
        return await db.Guides.CountAsync(g => g.AuthorId == authorId);
    }

    /// <summary>
    /// Returns count of user's published guides.
    /// </summary>
    public async Task<int> GetAuthorApprovedGuideCountAsync(ulong authorId)
    {
        return await db.Guides.CountAsync(g => g.AuthorId == authorId && g.Status == GuideStatus.Published);
    }

    /// <summary>
    /// Calculates max slots: 3 + TotalApprovedGuides
    /// </summary>
    public async Task<int> GetAuthorMaxSlotsAsync(ulong authorId)
    {
        var approvedCount = await GetAuthorApprovedGuideCountAsync(authorId);
        return 3 + approvedCount;
    }

    /// <summary>
    /// Checks if user can create new guides based on slot availability.
    /// </summary>
    public async Task<bool> CanCreateGuideAsync(ulong authorId)
    {
        var currentCount = await GetAuthorGuideCountAsync(authorId);
        var maxSlots = await GetAuthorMaxSlotsAsync(authorId);
        return currentCount < maxSlots;
    }
}
