using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guides.Endpoints;

/// <summary>
/// Edit a comment (author or admin)
/// </summary>
public class EditCommentEndpoint(CommentService commentService) : Endpoint<EditCommentRequest>
{
    public override void Configure()
    {
        Put("/comments/{CommentId}");
        Summary(s =>
        {
            s.Summary = "Edit a comment";
            s.Description = "Edit comment content. Authors can edit their own comments, admins can edit any.";
        });
    }

    public override async Task HandleAsync(EditCommentRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var isAdmin = User.IsModeratorOrHigher();
        var comment = await commentService.EditCommentAsync(req.CommentId, userId.Value, req.Content, isAdmin);
        
        if (comment == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        await Send.NoContentAsync(ct);
    }
}

public class EditCommentRequest
{
    public int CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class EditCommentValidator : Validator<EditCommentRequest>
{
    public EditCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content cannot be empty.")
            .MaximumLength(2048).WithMessage("Comment is too long (max 2048 chars).");
    }
}
