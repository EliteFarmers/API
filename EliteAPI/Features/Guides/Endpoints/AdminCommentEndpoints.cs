using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Comments.Mappers;
using EliteAPI.Features.Comments.Models.Dtos;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class ApproveCommentEndpoint(CommentService commentService) : Endpoint<ApproveCommentRequest>
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
        var success = await commentService.ApproveCommentAsync(req.CommentId, 0);
        
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        await Send.NoContentAsync(ct);
    }
}

public class ApproveCommentRequest
{
    public int CommentId { get; set; }
}

public class DeleteCommentEndpoint(CommentService commentService, UserManager userManager) : Endpoint<DeleteCommentRequest>
{
    public override void Configure()
    {
        Delete("/admin/comments/{CommentId}");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Delete a comment";
            s.Description = "Soft-deletes a comment.";
        });
    }

    public override async Task HandleAsync(DeleteCommentRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        var success = await commentService.DeleteCommentAsync(req.CommentId, user?.AccountId ?? 0);
        
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
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
        Policies(ApiUserPolicies.Moderator);
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
