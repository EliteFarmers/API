using EliteAPI.Data;
using EliteAPI.Features.AuditLogs.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Comments.Mappers;
using EliteAPI.Features.Comments.Models.Dtos;
using EliteAPI.Features.Notifications.Models;
using EliteAPI.Features.Notifications.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Endpoints;

public class ApproveCommentEndpoint(
    CommentService commentService,
    DataContext db,
    UserManager userManager,
    NotificationService notificationService,
    AuditLogService auditLogService) : Endpoint<ApproveCommentRequest>
{
    public override void Configure()
    {
        Post("/admin/comments/{CommentId}/approve");
        Policies(ApiUserPolicies.Support);

        Options(x => x.Accepts<ApproveCommentRequest>());

        Summary(s =>
        {
            s.Summary = "Approve a comment";
            s.Description = "Approves a pending comment, making it visible to all users.";
        });
    }

    public override async Task HandleAsync(ApproveCommentRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        
        var comment = await db.Comments
            .Include(c => c.Author)
                .ThenInclude(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(c => c.Id == req.CommentId, ct);
            
        if (comment == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var wasAlreadyApproved = comment.IsApproved;
        var hadDraft = !string.IsNullOrEmpty(comment.DraftContent);
        var success = await commentService.ApproveCommentAsync(req.CommentId, user?.AccountId ?? 0);
        
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var notificationType = hadDraft ? NotificationType.CommentEditApproved : NotificationType.CommentApproved;
        var title = hadDraft ? "Your comment edit was approved" : "Your comment was approved";
        
        var slug = SqidService.Encode(comment.TargetId);

        await notificationService.CreateAsync(
            comment.AuthorId,
            notificationType,
            title,
            "Your comment is now visible to everyone.",
            $"/guides/{slug}#comment-{SqidService.Encode(comment.Id)}");

        // Send new comment/reply notifications only on first approval (not edit approval)
        if (!wasAlreadyApproved && !hadDraft)
        {
            var commenterName = comment.Author?.MinecraftAccounts.FirstOrDefault()?.Name 
                                ?? comment.Author?.Username
                                ?? "[someone]";

            if (comment.ParentId.HasValue)
            {
                var parentComment = await db.Comments.FindAsync([comment.ParentId.Value], ct);
                if (parentComment != null && parentComment.AuthorId != comment.AuthorId)
                {
                    await notificationService.CreateAsync(
                        parentComment.AuthorId,
                        NotificationType.NewReply,
                        "New reply to your comment",
                        $"**{commenterName}** replied to your comment.",
                        $"/guides/{slug}#comment-{SqidService.Encode(comment.Id)}");
                }
            }

            var guide = await db.Guides
                .Include(g => g.ActiveVersion)
                .FirstOrDefaultAsync(g => g.Id == comment.TargetId, ct);
                
            if (guide != null && guide.AuthorId != comment.AuthorId)
            {
                await notificationService.CreateAsync(
                    guide.AuthorId,
                    NotificationType.NewComment,
                    "New comment on your guide",
                    $"**{commenterName}** commented on **{guide.ActiveVersion?.Title ?? "your guide"}**.",
                    $"/guides/{slug}#comment-{SqidService.Encode(comment.Id)}");
            }
        }

        await auditLogService.LogAsync(
            user?.AccountId ?? 0,
            hadDraft ? "comment_edit_approved" : "comment_approved",
            "Comment",
            req.CommentId.ToString(),
            $"Approved comment by {comment.Author?.MinecraftAccounts.FirstOrDefault()?.Name ?? comment.AuthorId.ToString()}");
        
        await Send.NoContentAsync(ct);
    }
}

public class ApproveCommentRequest
{
    public int CommentId { get; set; }
}

public class DeleteCommentEndpoint(
    CommentService commentService, 
    DataContext db,
    UserManager userManager,
    AuditLogService auditLogService) : Endpoint<DeleteCommentRequest>
{
    public override void Configure()
    {
        Delete("/admin/comments/{CommentId}");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Delete a comment";
            s.Description = "Deletes a comment.";
        });
    }

    public override async Task HandleAsync(DeleteCommentRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        
        var comment = await db.Comments
            .Include(c => c.Author)
                .ThenInclude(eliteAccount => eliteAccount.MinecraftAccounts)
            .FirstOrDefaultAsync(c => c.Id == req.CommentId, ct);
            
        if (comment == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        var success = await commentService.DeleteCommentAsync(req.CommentId, user?.AccountId ?? 0);
        
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await auditLogService.LogAsync(
            user?.AccountId ?? 0,
            "comment_deleted",
            "Comment",
            req.CommentId.ToString(),
            $"Deleted comment by {comment.Author?.MinecraftAccounts.FirstOrDefault()?.Name ?? comment.AuthorId.ToString()}");
        
        await Send.NoContentAsync(ct);
    }
}

public class DeleteCommentRequest
{
    public int CommentId { get; set; }
}

public class ListPendingCommentsEndpoint(CommentService commentService, CommentMapper mapper) : EndpointWithoutRequest<List<CommentDto>>
{
    public override void Configure()
    {
        Get("/admin/comments/pending");
        Policies(ApiUserPolicies.Support);
        Summary(s =>
        {
            s.Summary = "List all pending comments";
            s.Description = "Returns a list of all comments pending approval.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var comments = await commentService.GetAllPendingCommentsAsync();
        
        var response = comments.Select(c => mapper.ToDto(c, null, true, null)).ToList();
        
        await Send.OkAsync(response, ct);
    }
}

