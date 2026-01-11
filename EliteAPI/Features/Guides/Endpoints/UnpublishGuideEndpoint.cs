using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class UnpublishGuideEndpoint(GuideService guideService) : Endpoint<UnpublishGuideRequest>
{
    public override void Configure()
    {
        Post("/guides/{guideId}/unpublish");
        Options(x => x.Accepts<UnpublishGuideRequest>());
        Summary(s =>
        {
            s.Summary = "Unpublish a guide";
            s.Description = "Revert a published guide back to draft status. Only author or admin can unpublish.";
        });
        Description(b => b.Produces(204).Produces(401).Produces(403).Produces(404));
    }

    public override async Task HandleAsync(UnpublishGuideRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var isAdmin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);
        var success = await guideService.UnpublishGuideAsync(req.GuideId, userId.Value, isAdmin);
        
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public class UnpublishGuideRequest
{
    public int GuideId { get; set; }
}
