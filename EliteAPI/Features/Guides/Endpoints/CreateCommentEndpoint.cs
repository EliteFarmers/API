using EliteAPI.Data;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Comments.Mappers;
using EliteAPI.Features.Comments.Models.Dtos;
using EliteAPI.Features.Notifications.Models;
using EliteAPI.Features.Notifications.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Endpoints;

public class CreateCommentEndpoint(
    CommentService commentService, 
    CommentMapper mapper,
    DataContext db,
    NotificationService notificationService) : Endpoint<CreateCommentRequest, CommentDto>
{
    public override void Configure()
    {
        Post("/guides/{GuideId}/comments");
        Summary(s => 
        {
            s.Summary = "Post a comment";
            s.Description = "Create a new comment on a guide.";
        });
    }

    public override async Task HandleAsync(CreateCommentRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try 
        {
            var comment = await commentService.AddCommentAsync(req.GuideId, EliteAPI.Features.Comments.Models.CommentTargetType.Guide, userId.Value, req.Content, req.ParentId, req.LiftedElementId);
            
            var guide = await db.Guides
                .Include(g => g.ActiveVersion)
                .FirstOrDefaultAsync(g => g.Id == req.GuideId, ct);

            var commenterAccount = await db.Accounts
                .Include(a => a.MinecraftAccounts)
                .FirstOrDefaultAsync(a => a.Id == userId.Value, ct);
            var commenterName = commenterAccount?.MinecraftAccounts.FirstOrDefault()?.Name ?? "Someone";

            if (req.ParentId.HasValue)
            {
                var parentComment = await db.Comments.FindAsync([req.ParentId.Value], ct);
                if (parentComment != null && parentComment.AuthorId != userId.Value)
                {
                    await notificationService.CreateAsync(
                        parentComment.AuthorId,
                        NotificationType.NewReply,
                        "New reply to your comment",
                        $"**{commenterName}** replied to your comment.",
                        $"/guides/{guide?.Id}-{guide?.ActiveVersion?.Title?.ToLower().Replace(" ", "-")}#comment-{comment.Id}");
                }
            }
            else if (guide != null && guide.AuthorId != userId.Value)
            {
                await notificationService.CreateAsync(
                    guide.AuthorId,
                    NotificationType.NewComment,
                    "New comment on your guide",
                    $"**{commenterName}** commented on **{guide.ActiveVersion?.Title ?? "your guide"}**.",
                    $"/guides/{guide.Id}-{guide.ActiveVersion?.Title?.ToLower().Replace(" ", "-")}#comment-{comment.Id}");
            }
            
            await Send.OkAsync(mapper.ToDto(comment, userId.Value, User.IsSupportOrHigher(), null), ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            ThrowError(ex.Message);
        }
    }
}


public class CreateCommentRequest
{
    public int GuideId { get; set; }
    public int? ParentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? LiftedElementId { get; set; }
}

public class CreateCommentValidator : Validator<CreateCommentRequest>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content cannot be empty.")
            .MaximumLength(2048).WithMessage("Comment is too long (max 2048 chars).");
            
        RuleFor(x => x.LiftedElementId)
            .MaximumLength(128);
    }
}

