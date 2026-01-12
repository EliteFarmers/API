using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guides.Endpoints;

public class UpdateGuideEndpoint(GuideService guideService) : Endpoint<UpdateGuideRequest>
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
        var userId = User.GetDiscordId();
        if (userId is null)
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
        
        var isAuthor = guide.AuthorId == userId.Value;
        var isModerator = User.IsModeratorOrHigher();
        
        if (!isAuthor && !isModerator)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        await guideService.UpdateDraftAsync(req.Id, req.Title, req.Description, req.MarkdownContent, req.IconSkyblockId, req.Tags, req.RichBlocks);
        
        await Send.NoContentAsync(ct);
    }
}

public class UpdateGuideRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IconSkyblockId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MarkdownContent { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public GuideRichData? RichBlocks { get; set; }
}
