using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

public class GetAuctionPriceHistory(DataContext context) : Endpoint<GetAuctionPriceHistoryRequest, GetAuctionPriceHistoryResponse>
{

    public override void Configure()
    {
        Get("/resources/auctions/{skyblockId}/{variantKey}");
        AllowAnonymous();
        
        Summary(s => {
            s.Summary = "Get Auction History For Item";
        });
    }

    public override async Task HandleAsync(GetAuctionPriceHistoryRequest r, CancellationToken c)
    {
        var timespan = r.Timespan ?? "7d";
        var timespanDays = timespan switch
        {
            "1d" => 1,
            "3d" => 3,
            "7d" => 7,
            "14d" => 14,
            "30d" => 30,
            _ => 7
        };

        var variantKey = NormalizeVariantKey(r.VariantKey);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-timespanDays).ToUnixTimeMilliseconds();

        var history = await context.AuctionPriceHistories
            .AsNoTracking()
            .Where(h => h.SkyblockId == r.SkyblockId && h.VariantKey == variantKey && h.BucketStart >= cutoff)
            .OrderBy(h => h.BucketStart)
            .Select(h => new PriceHistoryDataPoint {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(h.BucketStart),
                LowestBinPrice = h.LowestBinPrice,
                AverageBinPrice = h.AverageBinPrice,
                BinListings = h.BinListings,
                LowestSalePrice = h.LowestSalePrice,
                AverageSalePrice = h.AverageSalePrice,
                SaleAuctions = h.SaleAuctions,
                ItemsSold = h.ItemsSold
            })
            .ToListAsync(c);

        var response = new GetAuctionPriceHistoryResponse
        {
            History = history
        };

        await Send.OkAsync(response, c);
    }

    private static string NormalizeVariantKey(string variantKey)
    {
        if (string.IsNullOrWhiteSpace(variantKey) || variantKey == "-") return string.Empty;
        return Uri.UnescapeDataString(variantKey);
    }
}

public class GetAuctionPriceHistoryRequest
{
    public required string SkyblockId { get; set; }
    public required string VariantKey { get; set; }
    public string? Timespan { get; set; }
}

public class GetAuctionPriceHistoryResponse
{
    public required List<PriceHistoryDataPoint> History { get; set; }
}

public class PriceHistoryDataPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal? LowestBinPrice { get; set; }
    public decimal? AverageBinPrice { get; set; }
    public int BinListings { get; set; }
    public decimal? LowestSalePrice { get; set; }
    public decimal? AverageSalePrice { get; set; }
    public int SaleAuctions { get; set; }
    public int ItemsSold { get; set; }
}