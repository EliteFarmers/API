using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Guides.Mappers;
using EliteAPI.Features.Guides.Models.Dtos;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class GetUserGuidesEndpoint(GuideService guideService, GuideMapper mapper) : Endpoint<GetUserGuidesRequest, List<UserGuideDto>>
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
        var isMod = User.IsSupportOrHigher();
        
        var includePrivate = isOwner || isMod;
        var guides = await guideService.GetUserGuidesAsync(req.AccountId, includePrivate);
        
        var response = guides.Select(mapper.ToUserGuideDto).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class GetUserGuidesRequest
{
    public ulong AccountId { get; set; }
}

