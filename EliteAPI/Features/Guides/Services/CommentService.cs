using FastEndpoints;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Guides.Models;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Services;

[RegisterService<CommentService>(LifeTime.Scoped)]
public class CommentService(DataContext db)
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
             // Assuming user.Permissions has Moderator flag, or we need to check ApiUser policies if passed context.
             // Since we only have userId here, check flags.
             if ((user!.Permissions & PermissionFlags.Moderator) == 0 && (user.Permissions & PermissionFlags.Admin) == 0)
             {
                 throw new UnauthorizedAccessException("Only moderators can lift comments.");
             }
        }

        var comment = new Comment
        {
            TargetId = targetId,
            TargetType = targetType,
            AuthorId = userId,
            Content = content,
            ParentId = parentId,
            LiftedElementId = liftedElementId,
            CreatedAt = DateTime.UtcNow,
            IsApproved = false // Default to pending
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return comment;
    }

    public async Task<List<Comment>> GetCommentsAsync(int targetId, EliteAPI.Features.Comments.Models.CommentTargetType targetType)
    {
        var comments = await db.Comments
            .Include(c => c.Author)
                .ThenInclude(a => a.MinecraftAccounts)
            .Include(c => c.Author)
                .ThenInclude(a => a.UserSettings)
            .Where(c => c.TargetId == targetId && c.TargetType == targetType && !c.IsDeleted)
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();
            
        return comments;
    }

    public async Task VoteAsync(int commentId, ulong userId, short value)
    {
        if (Math.Abs(value) != 1) throw new ArgumentException("Vote value must be 1 or -1");

        // Verify comment exists
        var commentExists = await db.Comments.AnyAsync(c => c.Id == commentId && !c.IsDeleted);
        if (!commentExists) throw new KeyNotFoundException("Comment not found.");

        var vote = await db.CommentVotes.FindAsync(commentId, userId);
        if (vote != null)
        {
            // Update existing
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

        await db.SaveChangesAsync();

        // Update aggregate score
        var score = await db.CommentVotes.Where(v => v.CommentId == commentId).SumAsync(v => v.Value);
        
        var comment = await db.Comments.FindAsync(commentId);
        if (comment != null)
        {
            comment.Score = score;
            await db.SaveChangesAsync();
        }
    }

    public async Task<bool> ApproveCommentAsync(int commentId, ulong moderatorId)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null || comment.IsDeleted) return false;
        
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
            .Where(c => c.TargetId == targetId && c.TargetType == targetType && !c.IsApproved && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> EditCommentAsync(int commentId, ulong editorId, string newContent, bool isAdmin)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null || comment.IsDeleted) return null;
        
        // Only author can edit, unless admin
        if (!isAdmin && comment.AuthorId != editorId) return null;
        
        comment.Content = newContent;
        comment.EditedAt = DateTime.UtcNow;
        if (isAdmin && comment.AuthorId != editorId)
        {
            comment.EditedByAdminId = editorId;
        }
        
        await db.SaveChangesAsync();
        return comment;
    }
}
