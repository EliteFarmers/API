using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Features.Guides.Endpoints;

public class GetGuideEndpoint(GuideService guideService, UserManager userManager) : Endpoint<GetGuideRequest, GetGuideResponse>
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
        if (guide == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        GuideVersion? versionToReturn;

        // If explicitly requesting draft
        if (req.Draft)
        {
            var user = await userManager.GetUserAsync(User);
            var canViewDraft = false;
             
            if (user?.AccountId != null)
            {
                if (guide.AuthorId == user.AccountId.Value) canViewDraft = true;
                if (User.IsInRole(ApiUserPolicies.Moderator) || User.IsInRole(ApiUserPolicies.Admin)) canViewDraft = true;
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
        }

        await Send.OkAsync(new GetGuideResponse
        {
            Id = guide.Id,
            Slug = guide.Slug ?? guideService.GetSlug(guide.Id),
            Title = versionToReturn.Title,
            Description = versionToReturn.Description,
            Content = versionToReturn.MarkdownContent,
            AuthorName = guide.Author.GetFormattedIgn(),
            AuthorUuid = guide.Author.MinecraftAccounts.FirstOrDefault(a => a.Selected)?.Id,
            CreatedAt = guide.CreatedAt,
            Score = guide.Score,
            Tags = guide.Tags.Select(t => t.Tag.Name).ToList(),
            IsDraft = req.Draft
        }, ct);
    }
}

public class GetGuideRequest
{
    public string Slug { get; set; } = string.Empty;
    
    [QueryParam]
    public bool Draft { get; set; } = false;
}

public class GetGuideResponse
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorUuid { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Score { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool IsDraft { get; set; }
}
