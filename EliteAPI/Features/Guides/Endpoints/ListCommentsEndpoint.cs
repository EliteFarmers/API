using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Comments.Mappers;
using EliteAPI.Features.Comments.Models.Dtos;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class ListCommentsEndpoint(CommentService commentService, GuideService guideService, CommentMapper mapper) : Endpoint<ListCommentsRequest, List<CommentDto>>
{
    public override void Configure()
    {
        Get("/guides/{Slug}/comments");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List comments for a guide";
            s.Description = "Returns all comments for a specific guide.";
        });
    }

    public override async Task HandleAsync(ListCommentsRequest req, CancellationToken ct)
    {
        var guideId = guideService.GetIdFromSlug(req.Slug);
        if (guideId == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetDiscordId();
        var isModerator = User.IsSupportOrHigher();
        
        var comments = await commentService.GetCommentsAsync(
            guideId.Value, 
            EliteAPI.Features.Comments.Models.CommentTargetType.Guide,
            userId,
            isModerator);

        Dictionary<int, short> userVotes = [];
        
        if (userId != null && comments.Count != 0)
        {
            userVotes = await commentService.GetUserVotesForCommentsAsync(comments.Select(c => c.Id), userId.Value);
        }

        var response = comments.Select(c => mapper.ToDto(c, userId, isModerator, userVotes.TryGetValue(c.Id, out var vote) ? vote : null)).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class ListCommentsRequest
{
    public string Slug { get; set; } = string.Empty;
}



