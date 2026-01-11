using EliteAPI.Data;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Services;

[RegisterService<GuideSearchService>(LifeTime.Scoped)]
public class GuideSearchService(DataContext db)
{
    public async Task<List<Guide>> SearchGuidesAsync(string? query, GuideType? type, List<int>? tagIds, GuideSort sort, int page = 1, int pageSize = 20, GuideStatus? status = GuideStatus.Published)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var q = db.Guides
            .Include(g => g.ActiveVersion)
            .Include(g => g.Author)
            .Include(g => g.Tags)
            .ThenInclude(gt => gt.Tag)
            .AsQueryable();

        if (status.HasValue)
        {
            q = q.Where(g => g.Status == status.Value);
        }

        if (type.HasValue)
        {
            q = q.Where(g => g.Type == type.Value);
        }

        if (tagIds != null && tagIds.Count > 0)
        {
            q = q.Where(g => g.Tags.Any(t => tagIds.Contains(t.TagId)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(g => EF.Functions.ILike(g.ActiveVersion!.Title, $"%{query}%") || 
                             EF.Functions.ILike(g.ActiveVersion!.Description, $"%{query}%"));
        }

        switch (sort)
        {
            case GuideSort.Newest:
                q = q.OrderByDescending(g => g.CreatedAt);
                break;
            case GuideSort.TopRated:
                q = q.OrderByDescending(g => g.Score);
                break;
            case GuideSort.Trending:
                // TODO: Implement trending
                q = q.OrderByDescending(g => g.Score);
                break;
        }

        return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }
}

[JsonStringEnum]
public enum GuideSort
{
    Newest,
    TopRated,
    Trending
}
