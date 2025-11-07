using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetPopularAuctionsRequest
{
    public string? Timespan { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

internal sealed class GetPopularAuctionsResponse
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required long TotalCount { get; set; }
    public required int TotalPages { get; set; }
    public List<PopularAuctionItemDto> Items { get; set; } = [];
}

internal sealed class PopularAuctionItemDto
{
    public required string SkyblockId { get; set; }
    public string? Name { get; set; }
    public int ItemsSold { get; set; }
    public int SaleAuctions { get; set; }
    public int BinListings { get; set; }
    public decimal? AverageSalePrice { get; set; }
    public decimal? AverageBinPrice { get; set; }
    public decimal? LowestSalePrice { get; set; }
    public decimal? LowestBinPrice { get; set; }
}

internal sealed class GetPopularAuctionsEndpoint(DataContext context)
    : Endpoint<GetPopularAuctionsRequest, GetPopularAuctionsResponse>
{
    public override void Configure() {
        Get("/resources/auctions/popular");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "List Popular Auction Items";
            s.Description = "Returns a paginated list of auction items ranked by recent trading volume.";
        });
    }

    public override async Task HandleAsync(GetPopularAuctionsRequest req, CancellationToken ct) {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = Math.Clamp(req.PageSize, 1, 50);

        var timespanDays = ParseTimespan(req.Timespan);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-timespanDays).ToUnixTimeMilliseconds();

        var aggregatedQuery = context.AuctionPriceHistories
            .AsNoTracking()
            .Where(h => h.BucketStart >= cutoff);

        var projectionQuery = aggregatedQuery
            .GroupBy(h => h.SkyblockId)
            .Select(g => new PopularAuctionProjection {
                SkyblockId = g.Key,
                ItemsSold = g.Sum(x => x.ItemsSold),
                SaleAuctions = g.Sum(x => x.SaleAuctions),
                BinListings = g.Sum(x => x.BinListings),
                AverageSalePrice = g.Average(x => x.AverageSalePrice),
                AverageBinPrice = g.Average(x => x.AverageBinPrice),
                LowestSalePrice = g.Min(x => x.LowestSalePrice),
                LowestBinPrice = g.Min(x => x.LowestBinPrice)
            })
            .Where(x => x.ItemsSold > 0 || x.BinListings > 0);

        var totalCount = await projectionQuery.LongCountAsync(ct);
        if (totalCount == 0) {
            await Send.OkAsync(new GetPopularAuctionsResponse {
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            }, ct);
            return;
        }

        var skip = (page - 1) * pageSize;

        var pageResults = await projectionQuery
            .OrderByDescending(x => x.ItemsSold)
            .ThenByDescending(x => x.BinListings)
            .ThenBy(x => x.SkyblockId)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        var ids = pageResults.Select(r => r.SkyblockId).Distinct().ToList();

        var nameLookup = ids.Count == 0
            ? new Dictionary<string, string?>()
            : await context.SkyblockItems
                .AsNoTracking()
                .Where(i => ids.Contains(i.ItemId))
                .Select(i => new { i.ItemId, i.Data })
                .ToDictionaryAsync(i => i.ItemId, i => i.Data?.Name, ct);

        var items = pageResults.Select(r => new PopularAuctionItemDto {
            SkyblockId = r.SkyblockId,
            Name = nameLookup.GetValueOrDefault(r.SkyblockId),
            ItemsSold = r.ItemsSold,
            SaleAuctions = r.SaleAuctions,
            BinListings = r.BinListings,
            AverageSalePrice = r.AverageSalePrice,
            AverageBinPrice = r.AverageBinPrice,
            LowestSalePrice = r.LowestSalePrice,
            LowestBinPrice = r.LowestBinPrice
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await Send.OkAsync(new GetPopularAuctionsResponse {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        }, ct);
    }

    private static int ParseTimespan(string? timespan) {
        return timespan switch {
            "1d" => 1,
            "3d" => 3,
            "14d" => 14,
            "30d" => 30,
            _ => 7
        };
    }

    private sealed class PopularAuctionProjection
    {
        public required string SkyblockId { get; init; }
        public int ItemsSold { get; init; }
        public int SaleAuctions { get; init; }
        public int BinListings { get; init; }
        public decimal? AverageSalePrice { get; init; }
        public decimal? AverageBinPrice { get; init; }
        public decimal? LowestSalePrice { get; init; }
        public decimal? LowestBinPrice { get; init; }
    }
}
