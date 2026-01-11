using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Guides.Mappers;
using EliteAPI.Features.Guides.Models.Dtos;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class GetGuideEndpoint(GuideService guideService, GuideMapper mapper) : Endpoint<GetGuideRequest, FullGuideDto>
{
    public override void Configure()
    {
        Get("/guides/{Slug}");
        AllowAnonymous();
        Summary(s => 
        {
            s.Summary = "Get a guide";
            s.Description = "Retrieve a guide by its slug. Use ?draft=true to view draft version (requires author/mod permission).";
        });
    }

    public override async Task HandleAsync(GetGuideRequest req, CancellationToken ct)
    {
        var guide = await guideService.GetBySlugAsync(req.Slug);
        if (guide == null || guide.IsDeleted)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetDiscordId();
        GuideVersion? versionToReturn;

        // If explicitly requesting draft
        if (req.Draft)
        {
            var canViewDraft = false;
             
            if (userId != null)
            {
                if (guide.AuthorId == userId.Value) canViewDraft = true;
                if (User.IsSupportOrHigher()) canViewDraft = true;
            }

            if (!canViewDraft || guide.DraftVersion == null)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
            
            versionToReturn = guide.DraftVersion;
        }
        else
        {
            // Default: return published version
            if (guide.ActiveVersion == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            versionToReturn = guide.ActiveVersion;
            
            // Increment view count for authenticated users viewing published version
            if (userId != null)
            {
                await guideService.IncrementViewCountAsync(guide.Id);
            }
        }

        // Get user-specific data if authenticated
        short? userVote = null;
        bool? isBookmarked = null;
        if (userId != null)
        {
            userVote = await guideService.GetUserVoteAsync(guide.Id, userId.Value);
            isBookmarked = await guideService.IsBookmarkedAsync(guide.Id, userId.Value);
        }

        await Send.OkAsync(mapper.ToFullGuideDto(guide, versionToReturn, userVote, isBookmarked), ct);
    }
}

public class GetGuideRequest
{
    public string Slug { get; set; } = string.Empty;
    
    [QueryParam]
    public bool Draft { get; set; } = false;
}

