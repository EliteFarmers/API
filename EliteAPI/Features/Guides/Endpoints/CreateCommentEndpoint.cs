using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Comments.Mappers;
using EliteAPI.Features.Comments.Models.Dtos;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guides.Endpoints;

public class CreateCommentEndpoint(
    CommentService commentService, 
    CommentMapper mapper) : Endpoint<CreateCommentRequest, CommentDto>
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


