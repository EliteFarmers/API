using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Features.Guides.Endpoints;

public class UpdateGuideEndpoint(GuideService guideService, UserManager userManager) : Endpoint<UpdateGuideRequest>
{
    public override void Configure()
    {
        Put("/guides/{Id}");
        Summary(s =>
        {
            s.Summary = "Update a guide draft";
            s.Description = "Update the draft version of a guide. Only the author can update their own guide.";
        });
    }

    public override async Task HandleAsync(UpdateGuideRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var guide = await guideService.GetByIdAsync(req.Id);
        if (guide == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        // Authorization: Only author or moderators can update
        var isAuthor = guide.AuthorId == user.AccountId.Value;
        var isModerator = User.IsInRole(ApiUserPolicies.Moderator) || User.IsInRole(ApiUserPolicies.Admin);
        
        if (!isAuthor && !isModerator)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        await guideService.UpdateDraftAsync(req.Id, req.Title, req.Description, req.MarkdownContent, req.RichBlocks);
        
        await Send.NoContentAsync(ct);
    }
}

public class UpdateGuideRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MarkdownContent { get; set; } = string.Empty;
    public GuideRichData? RichBlocks { get; set; }
}
