using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class DeleteGuideEndpoint(GuideService guideService) : Endpoint<DeleteGuideRequest>
{
    public override void Configure()
    {
        Delete("/guides/{id}");
        Summary(s =>
        {
            s.Summary = "Delete a guide";
            s.Description = "Soft delete a guide. Only the author or an admin can delete.";
        });
        Description(b => b.Produces(204).Produces(401).Produces(403).Produces(404));
    }

    public override async Task HandleAsync(DeleteGuideRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var isAdmin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);
        var success = await guideService.DeleteGuideAsync(req.Id, userId.Value, isAdmin);
        
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public class DeleteGuideRequest
{
    public int Id { get; set; }
}
