using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class ListGuidesEndpoint(GuideSearchService searchService) : Endpoint<ListGuidesRequest, List<GuideResponse>>
{
    public override void Configure()
    {
        Get("/guides");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List guides";
            s.Description = "Search and list published guides with optional filtering and sorting.";
        });
    }

    public override async Task HandleAsync(ListGuidesRequest req, CancellationToken ct)
    {
        var guides = await searchService.SearchGuidesAsync(req.Query, req.Type, req.Tags, req.Sort, req.Page, req.PageSize);
        
        var response = guides.Select(g => new GuideResponse
        {
            Id = g.Id,
            Slug = g.Slug ?? g.Id.ToString(),
            Title = g.ActiveVersion?.Title ?? "Untitled",
            Status = g.Status.ToString()
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class ListGuidesRequest
{
    public string? Query { get; set; }
    public GuideType? Type { get; set; }
    public List<int>? Tags { get; set; }
    public GuideSort Sort { get; set; } = GuideSort.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
