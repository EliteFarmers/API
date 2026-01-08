using EliteAPI.Features.Guides.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Features.Guides.Endpoints;

public class CreateCommentEndpoint(CommentService commentService, UserManager userManager) : Endpoint<CreateCommentRequest>
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
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try 
        {
            var comment = await commentService.AddCommentAsync(req.GuideId, EliteAPI.Features.Comments.Models.CommentTargetType.Guide, user.AccountId.Value, req.Content, req.ParentId, req.LiftedElementId);
            
            await Send.OkAsync(new CreateCommentResponse
            {
                Id = comment.Id,
                Sqid = EliteAPI.Features.Common.Services.SqidService.Encode(comment.Id),
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            }, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            ThrowError(ex.Message);
        }
    }
}

public class CreateCommentResponse
{
    public int Id { get; set; }
    public string Sqid { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
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
