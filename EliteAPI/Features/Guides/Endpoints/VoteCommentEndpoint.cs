using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guides.Endpoints;

public class VoteCommentEndpoint(CommentService commentService) : Endpoint<VoteCommentRequest>
{
    public override void Configure()
    {
        Post("/comments/{CommentId}/vote");
        Summary(s => 
        {
            s.Summary = "Vote on a comment";
            s.Description = "Cast an upvote (+1) or downvote (-1) on a comment.";
        });
    }

    public override async Task HandleAsync(VoteCommentRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            await commentService.VoteAsync(req.CommentId, userId.Value, req.Value);
            await Send.NoContentAsync(ct);
        }
        catch (ArgumentException)
        {
            ThrowError("Invalid vote value");
        }
    }
}

public class VoteCommentRequest
{
    public int CommentId { get; set; }
    public short Value { get; set; }
}

public class VoteCommentValidator : Validator<VoteCommentRequest>
{
    public VoteCommentValidator()
    {
        RuleFor(x => x.Value)
            .Must(v => v == 1 || v == -1)
            .WithMessage("Vote value must be either 1 (upvote) or -1 (downvote).");
    }
}
