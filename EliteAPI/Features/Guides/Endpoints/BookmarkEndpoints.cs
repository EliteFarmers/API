using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Guides.Mappers;
using EliteAPI.Features.Guides.Models.Dtos;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class BookmarkGuideEndpoint(GuideService guideService) : Endpoint<BookmarkRequest>
{
    public override void Configure()
    {
        Post("/guides/{guideId}/bookmark");
        Options(x => x.Accepts<BookmarkRequest>());
        Summary(s =>
        {
            s.Summary = "Bookmark a guide";
            s.Description = "Add a guide to user's bookmarks/favorites.";
        });
        Description(b => b.Produces(204).Produces(400).Produces(401));
    }

    public override async Task HandleAsync(BookmarkRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var success = await guideService.BookmarkGuideAsync(req.GuideId, userId.Value);
        if (!success)
        {
            ThrowError("Guide not found or already bookmarked.");
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public class UnbookmarkGuideEndpoint(GuideService guideService) : Endpoint<BookmarkRequest>
{
    public override void Configure()
    {
        Delete("/guides/{guideId}/bookmark");
        Summary(s =>
        {
            s.Summary = "Remove bookmark";
            s.Description = "Remove a guide from user's bookmarks.";
        });
        Description(b => b.Produces(204).Produces(404).Produces(401));
    }

    public override async Task HandleAsync(BookmarkRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var success = await guideService.UnbookmarkGuideAsync(req.GuideId, userId.Value);
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public class GetUserBookmarksEndpoint(GuideService guideService, GuideMapper mapper) : Endpoint<GetUserBookmarksRequest, List<UserGuideDto>>
{
    public override void Configure()
    {
        Get("/users/{accountId}/bookmarks");
        Summary(s =>
        {
            s.Summary = "Get user's bookmarked guides";
            s.Description = "Returns all guides bookmarked by the user. Only the owner can see their bookmarks.";
        });
    }

    public override async Task HandleAsync(GetUserBookmarksRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Only the owner can view their own bookmarks
        if (userId != req.AccountId && !User.IsInRole(ApiUserPolicies.Admin))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var guides = await guideService.GetUserBookmarksAsync(req.AccountId);
        
        var response = guides.Select(mapper.ToUserGuideDto).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class BookmarkRequest
{
    public int GuideId { get; set; }
}

public class GetUserBookmarksRequest
{
    public ulong AccountId { get; set; }
}
