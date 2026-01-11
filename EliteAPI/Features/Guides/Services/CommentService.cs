using FastEndpoints;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Guides.Models;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Services;

[RegisterService<CommentService>(LifeTime.Scoped)]
public class CommentService(DataContext db, HtmlSanitizer sanitizer)
{
    public async Task<Comment> AddCommentAsync(int targetId, EliteAPI.Features.Comments.Models.CommentTargetType targetType, ulong userId, string content, int? parentId, string? liftedElementId)
    {
        // Check permissions
        var user = await db.Accounts.FindAsync(userId);
        if (user != null && (user.Permissions & PermissionFlags.RestrictedFromComments) != 0)
        {
            throw new UnauthorizedAccessException("You are restricted from posting comments.");
        }
        
        // Restrict LiftedElementId to Moderators/Admins
        if (!string.IsNullOrEmpty(liftedElementId))
        {
             if ((user!.Permissions & PermissionFlags.Moderator) == 0 && (user.Permissions & PermissionFlags.Admin) == 0)
             {
                 throw new UnauthorizedAccessException("Only moderators can lift comments.");
             }
        }

        // Sanitize HTML content
        var sanitizedContent = sanitizer.Sanitize(content);

        var comment = new Comment
        {
            TargetId = targetId,
            TargetType = targetType,
            AuthorId = userId,
            Content = sanitizedContent,
            ParentId = parentId,
            LiftedElementId = liftedElementId,
            CreatedAt = DateTime.UtcNow,
            IsApproved = false // Default to pending
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return comment;
    }

    public async Task<List<Comment>> GetCommentsAsync(
        int targetId, 
        EliteAPI.Features.Comments.Models.CommentTargetType targetType,
        ulong? viewingUserId = null,
        bool isModerator = false)
    {
        // Include deleted comments to preserve thread structure
        var comments = await db.Comments
            .Include(c => c.Author)
                .ThenInclude(a => a.MinecraftAccounts)
            .Include(c => c.Author)
                .ThenInclude(a => a.UserSettings)
            .Where(c => c.TargetId == targetId && c.TargetType == targetType)
            .Where(c => c.IsDeleted 
                || c.IsApproved 
                || (viewingUserId != null && c.AuthorId == viewingUserId) 
                || isModerator)
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();
            
        return comments;
    }

    public async Task VoteAsync(int commentId, ulong userId, short value)
    {
        // Verify comment exists
        var commentExists = await db.Comments.AnyAsync(c => c.Id == commentId && !c.IsDeleted);
        if (!commentExists) throw new KeyNotFoundException("Comment not found.");

        var vote = await db.CommentVotes.FindAsync(commentId, userId);

        if (value == 0)
        {
            if (vote != null)
            {
                db.CommentVotes.Remove(vote);
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
                vote = new CommentVote
                {
                    CommentId = commentId,
                    UserId = userId,
                    Value = value
                };
                db.CommentVotes.Add(vote);
            }
        }

        await db.SaveChangesAsync();

        // Update aggregate score
        var score = await db.CommentVotes
            .Where(v => v.CommentId == commentId)
            .SumAsync(v => (int)v.Value);

        await db.Comments
            .Where(c => c.Id == commentId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Score, score));
    }

    public async Task<bool> ApproveCommentAsync(int commentId, ulong moderatorId)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null || comment.IsDeleted) return false;
        
        // If there's a pending draft edit, apply it
        if (!string.IsNullOrEmpty(comment.DraftContent))
        {
            comment.Content = comment.DraftContent;
            comment.DraftContent = null;
            comment.EditedAt = DateTime.UtcNow;
        }
        
        comment.IsApproved = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, ulong moderatorId)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null) return false;
        
        comment.IsDeleted = true;
        comment.EditedByAdminId = moderatorId;
        comment.EditedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Comment>> GetPendingCommentsAsync(int targetId, EliteAPI.Features.Comments.Models.CommentTargetType targetType)
    {
        return await db.Comments
            .Include(c => c.Author)
                .ThenInclude(a => a.MinecraftAccounts)
            .Where(c => c.TargetId == targetId && c.TargetType == targetType && !c.IsDeleted)
            .Where(c => !c.IsApproved || c.DraftContent != null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Comment>> GetAllPendingCommentsAsync()
    {
        return await db.Comments
            .Include(c => c.Author)
                .ThenInclude(a => a.MinecraftAccounts)
            .Where(c => !c.IsDeleted)
            .Where(c => !c.IsApproved || c.DraftContent != null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> EditCommentAsync(int commentId, ulong editorId, string newContent, bool isAdmin)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null || comment.IsDeleted) return null;
        
        // Only author can edit, unless admin
        if (!isAdmin && comment.AuthorId != editorId) return null;
        
        // Sanitize HTML content
        var sanitizedContent = sanitizer.Sanitize(newContent);
        
        // Admin edits bypass draft workflow
        if (isAdmin && comment.AuthorId != editorId)
        {
            comment.Content = sanitizedContent;
            comment.EditedAt = DateTime.UtcNow;
            comment.EditedByAdminId = editorId;
        }
        else
        {
            // Author edits go to draft for re-approval (if comment was already approved)
            if (comment.IsApproved)
            {
                comment.DraftContent = sanitizedContent;
            }
            else
            {
                // Not yet approved, direct edit is fine
                comment.Content = sanitizedContent;
            }
            comment.EditedAt = DateTime.UtcNow;
        }
        
        await db.SaveChangesAsync();
        return comment;
    }
    
    public async Task<Dictionary<int, short>> GetUserVotesForCommentsAsync(IEnumerable<int> commentIds, ulong userId)
    {
        return await db.CommentVotes
            .Where(v => commentIds.Contains(v.CommentId) && v.UserId == userId)
            .ToDictionaryAsync(v => v.CommentId, v => v.Value);
    }
}

