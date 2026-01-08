using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class GetUserGuidesEndpoint(GuideService guideService) : Endpoint<GetUserGuidesRequest, List<UserGuideResponse>>
{
    public override void Configure()
    {
        Get("/users/{accountId}/guides");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get guides by user";
            s.Description = "Returns all guides by a specific user. Shows only published guides for anonymous users. Author sees all their own guides.";
        });
    }

    public override async Task HandleAsync(GetUserGuidesRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        var isOwner = userId == req.AccountId;
        var isMod = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);
        
        var includePrivate = isOwner || isMod;
        var guides = await guideService.GetUserGuidesAsync(req.AccountId, includePrivate);
        
        var response = guides.Select(g => new UserGuideResponse
        {
            Id = g.Id,
            Slug = g.Slug ?? "",
            Title = (g.ActiveVersion ?? g.DraftVersion)?.Title ?? "Untitled",
            Description = (g.ActiveVersion ?? g.DraftVersion)?.Description ?? "",
            Type = g.Type.ToString(),
            Status = g.Status.ToString(),
            Score = g.Score,
            ViewCount = g.ViewCount,
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class GetUserGuidesRequest
{
    public ulong AccountId { get; set; }
}

public class UserGuideResponse
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public int Score { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
