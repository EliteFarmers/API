using EliteAPI.Data;
using EliteAPI.Features.Guides.Models;
using Microsoft.EntityFrameworkCore;
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
        var id = GetIdFromSlug(slug);
        if (id == null) return null;

        return await db.Guides
            .Include(g => g.Author)
            .ThenInclude(a => a.MinecraftAccounts)
            .Include(g => g.Author)
            .ThenInclude(a => a.UserSettings)
            .Include(g => g.ActiveVersion)
            .Include(g => g.DraftVersion)
            .Include(g => g.Tags)
            .ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(g => g.Id == id.Value);
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

    public async Task UpdateDraftAsync(int guideId, string title, string description, string markdown, string? iconSkyblockId,
        GuideRichData? richData)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null) throw new KeyNotFoundException("Guide not found");

        var draft = await db.GuideVersions.FindAsync(guide.DraftVersionId);
        if (draft == null)
        {
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
        }

        guide.IconSkyblockId = iconSkyblockId;
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

    public async Task<bool> RejectGuideAsync(int guideId, ulong adminId, string? reason = null)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null) return false;

        guide.Status = GuideStatus.Rejected;
        guide.RejectionReason = reason;
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task VoteGuideAsync(int guideId, ulong userId, short value)
    {
        var guideExists = await db.Guides.AnyAsync(g => g.Id == guideId && !g.IsDeleted);
        if (!guideExists) throw new KeyNotFoundException("Guide not found.");

        var vote = await db.GuideVotes.FindAsync(guideId, userId);

        if (value == 0)
        {
            if (vote != null)
            {
                db.GuideVotes.Remove(vote);
            }
        }
        else
        {
            if (Math.Abs(value) != 1) throw new ArgumentException("Vote value must be 1, -1, or 0 (remove)");

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
        }

        await db.SaveChangesAsync();

        // Update aggregate score
        // Recalculate score from GuideVotes for consistency
        var score = await db.GuideVotes
            .Where(v => v.GuideId == guideId)
            .SumAsync(v => (int)v.Value);

        await db.Guides
            .Where(g => g.Id == guideId)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.Score, score));
    }

    /// <summary>
    /// Returns count of user's non-deleted guides for slot management.
    /// </summary>
    public async Task<int> GetAuthorGuideCountAsync(ulong authorId)
    {
        return await db.Guides.CountAsync(g => g.AuthorId == authorId && !g.IsDeleted);
    }

    /// <summary>
    /// Returns count of user's published guides.
    /// </summary>
    public async Task<int> GetAuthorApprovedGuideCountAsync(ulong authorId)
    {
        return await db.Guides.CountAsync(g =>
            g.AuthorId == authorId && g.Status == GuideStatus.Published && !g.IsDeleted);
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

    /// <summary>
    /// Soft delete a guide. Only author or admin can delete.
    /// </summary>
    public async Task<bool> DeleteGuideAsync(int guideId, ulong userId, bool isAdmin)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null || guide.IsDeleted) return false;

        // Only author or admin can delete
        if (!isAdmin && guide.AuthorId != userId) return false;

        guide.IsDeleted = true;
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Revert a published guide back to draft status.
    /// </summary>
    public async Task<bool> UnpublishGuideAsync(int guideId, ulong userId, bool isAdmin)
    {
        var guide = await db.Guides.FindAsync(guideId);
        if (guide == null || guide.IsDeleted) return false;
        if (guide.Status != GuideStatus.Published) return false;

        // Only author or admin can unpublish
        if (!isAdmin && guide.AuthorId != userId) return false;

        guide.Status = GuideStatus.Draft;
        guide.ActiveVersionId = null; // Clear published version
        guide.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Increment view count (auth-only).
    /// </summary>
    public async Task IncrementViewCountAsync(int guideId)
    {
        await db.Guides.Where(g => g.Id == guideId)
            .ExecuteUpdateAsync(g => g.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
    }

    /// <summary>
    /// Get all guides by a specific author.
    /// </summary>
    public async Task<List<Guide>> GetUserGuidesAsync(ulong authorId, bool includePrivate = false)
    {
        var query = db.Guides
            .Include(g => g.ActiveVersion)
            .Include(g => g.DraftVersion)
            .Include(g => g.Tags).ThenInclude(t => t.Tag)
            .Where(g => g.AuthorId == authorId && !g.IsDeleted);

        if (!includePrivate)
        {
            query = query.Where(g => g.Status == GuideStatus.Published);
        }

        return await query.OrderByDescending(g => g.UpdatedAt ?? g.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Bookmark a guide for the user.
    /// </summary>
    public async Task<bool> BookmarkGuideAsync(int guideId, ulong userId)
    {
        var exists = await db.GuideBookmarks.AnyAsync(b => b.GuideId == guideId && b.UserId == userId);
        if (exists) return false;

        var guideExists = await db.Guides.AnyAsync(g => g.Id == guideId && !g.IsDeleted);
        if (!guideExists) return false;

        db.GuideBookmarks.Add(new GuideBookmark
        {
            GuideId = guideId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Remove a bookmark.
    /// </summary>
    public async Task<bool> UnbookmarkGuideAsync(int guideId, ulong userId)
    {
        var bookmark = await db.GuideBookmarks.FindAsync(userId, guideId);
        if (bookmark == null) return false;

        db.GuideBookmarks.Remove(bookmark);
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get all bookmarked guides for a user.
    /// </summary>
    public async Task<List<Guide>> GetUserBookmarksAsync(ulong userId)
    {
        return await db.GuideBookmarks
            .Where(b => b.UserId == userId)
            .Include(b => b.Guide).ThenInclude(g => g.ActiveVersion)
            .Include(b => b.Guide).ThenInclude(g => g.Author).ThenInclude(a => a.MinecraftAccounts)
            .Select(b => b.Guide)
            .Where(g => !g.IsDeleted && g.Status == GuideStatus.Published)
            .ToListAsync();
    }

    /// <summary>
    /// Get user's vote on a specific guide.
    /// </summary>
    public async Task<short?> GetUserVoteAsync(int guideId, ulong userId)
    {
        var vote = await db.GuideVotes.FindAsync(guideId, userId);
        return vote?.Value;
    }

    /// <summary>
    /// Check if user has bookmarked a guide.
    /// </summary>
    public async Task<bool> IsBookmarkedAsync(int guideId, ulong userId)
    {
        return await db.GuideBookmarks.AnyAsync(b => b.GuideId == guideId && b.UserId == userId);
    }
}