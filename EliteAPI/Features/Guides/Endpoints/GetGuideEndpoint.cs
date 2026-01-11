using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class GetGuideEndpoint(GuideService guideService) : Endpoint<GetGuideRequest, GetGuideResponse>
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

        await Send.OkAsync(new GetGuideResponse
        {
            Id = guide.Id,
            Slug = guide.Slug ?? guideService.GetSlug(guide.Id),
            Title = versionToReturn.Title,
            Description = versionToReturn.Description,
            Content = versionToReturn.MarkdownContent,
            AuthorName = guide.Author.GetFormattedIgn(),
            AuthorId = guide.AuthorId,
            AuthorUuid = guide.Author.MinecraftAccounts.FirstOrDefault(a => a.Selected)?.Id,
            AuthorAvatar = guide.Author.HasMinecraftAccount() ? null : guide.Author.Avatar,
            CreatedAt = guide.CreatedAt,
            Score = guide.Score,
            ViewCount = guide.ViewCount,
            Tags = guide.Tags.Select(t => t.Tag.Name).ToList(),
            IsDraft = req.Draft,
            UserVote = userVote,
            IsBookmarked = isBookmarked,
            RejectionReason = guide.Status == GuideStatus.Rejected ? guide.RejectionReason : null
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
    public string? AuthorAvatar { get; set; }
    public ulong AuthorId { get; set; }
    public string? AuthorUuid { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Score { get; set; }
    public int ViewCount { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool IsDraft { get; set; }
    
    /// <summary>
    /// Current user's vote on this guide (+1, -1, or null if not voted).
    /// </summary>
    public short? UserVote { get; set; }
    
    /// <summary>
    /// Whether the current user has bookmarked this guide.
    /// </summary>
    public bool? IsBookmarked { get; set; }
    
    /// <summary>
    /// Reason for rejection (only present for rejected guides visible to author).
    /// </summary>
    public string? RejectionReason { get; set; }
}
