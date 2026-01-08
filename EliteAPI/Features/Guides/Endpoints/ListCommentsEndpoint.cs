using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class ListCommentsEndpoint(CommentService commentService, GuideService guideService) : Endpoint<ListCommentsRequest, List<CommentResponse>>
{
    public override void Configure()
    {
        Get("/guides/{Slug}/comments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListCommentsRequest req, CancellationToken ct)
    {
        var guideId = guideService.GetIdFromSlug(req.Slug);
        if (guideId == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var comments = await commentService.GetCommentsAsync(guideId.Value, EliteAPI.Features.Comments.Models.CommentTargetType.Guide);

        var response = comments.Select(c => new CommentResponse
        {
            Id = c.Id,
            Sqid = SqidService.Encode(c.Id),
            ParentId = c.ParentId,
            Content = c.Content,
            AuthorName = c.Author.GetFormattedIgn(),
            CreatedAt = c.CreatedAt,
            Score = c.Score,
            LiftedElementId = c.LiftedElementId
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class ListCommentsRequest
{
    public string Slug { get; set; } = string.Empty;
}

public class CommentResponse
{
    public int Id { get; set; }
    public string Sqid { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Score { get; set; }
    public string? LiftedElementId { get; set; }
}
