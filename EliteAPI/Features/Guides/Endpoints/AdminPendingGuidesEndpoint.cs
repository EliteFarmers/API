using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Guides.Mappers;
using EliteAPI.Features.Guides.Models.Dtos;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class AdminPendingGuidesEndpoint(GuideSearchService searchService, GuideMapper mapper) : EndpointWithoutRequest<List<GuideDto>>
{
    public override void Configure()
    {
        Get("/admin/guides/pending");
        Policies(ApiUserPolicies.Support);
        Summary(s => 
        {
            s.Summary = "Get pending guides";
            s.Description = "Retrieves a list of guides waiting for approval.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var guides = await searchService.SearchGuidesAsync(null, null, null, GuideSort.Newest, 1, 100, Models.GuideStatus.PendingApproval);
        
        var response = guides.Select(mapper.ToDto).ToList();

        await Send.OkAsync(response, ct);
    }
}
