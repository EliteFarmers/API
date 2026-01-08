using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class AdminPendingGuidesEndpoint(GuideSearchService searchService) : EndpointWithoutRequest<List<GuideResponse>>
{
    public override void Configure()
    {
        Get("/admin/guides/pending");
        Policies(ApiUserPolicies.Moderator);
        Summary(s => 
        {
            s.Summary = "Get pending guides";
            s.Description = "Retrieves a list of guides waiting for approval.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var guides = await searchService.SearchGuidesAsync(null, null, null, GuideSort.Newest, 1, 100, Models.GuideStatus.PendingApproval);
        
        var response = guides.Select(g => new GuideResponse
        {
            Id = g.Id,
            Slug = g.Slug ?? g.Id.ToString(),
            Title = g.DraftVersion?.Title ?? "Untitled", // Use DraftVersion for pending guides
            Status = g.Status.ToString()
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}
