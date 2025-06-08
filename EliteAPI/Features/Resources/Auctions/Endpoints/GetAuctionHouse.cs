using System.ComponentModel;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using HypixelAPI;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseRequest
{
    [QueryParam, DefaultValue(0)]
    public int Page { get; set; } = 0;
}

internal sealed class GetAuctionHouseProductsEndpoint(
    DataContext context,
    IHypixelApi hypixelApi,
    AuctionsIngestionService auctionsIngestionService
) : Endpoint<GetAuctionHouseRequest, AuctionHouseDto> {
	
    public override void Configure() {
        Get("/resources/auctions");
        AllowAnonymous();
        Version(0);

        Description(x => x.Accepts<GetAuctionHouseRequest>());
        
        Summary(s => {
            s.Summary = "Get Auction House";
            s.Description = "Get lowest auction house prices.";
        });
		
        // Options(o => {
        //     o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("auctions"));
        // });
    }

    public override async Task HandleAsync(GetAuctionHouseRequest request, CancellationToken c) {
        await auctionsIngestionService.IngestAndStageAuctionDataAsync(c);
        
        
        var page1 = await hypixelApi.FetchAuctionHouse(request.Page);

        if (!page1.IsSuccessful)
        {
            ThrowError("Get Auction House Failed");
        }

        var data = page1.Content;
        var response = new AuctionHouseDto()
        {
            Success = data.Success,
            Page = data.Page,
            TotalPages = data.TotalPages,
            TotalAuctions = data.TotalAuctions,
            LastUpdated = data.LastUpdated,
            Auctions = await data.Auctions.ToAsyncEnumerable()
                .SelectAwait(async a => new AuctionHouseAuctionDto()
                {
                    Item = await NbtParser.NbtToItem(a.ItemBytes)
                }).ToListAsync(cancellationToken: c)
        };
        
        await SendAsync(response, cancellation: c);
    }
}

internal sealed class AuctionHouseDto
{
    public bool Success { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalAuctions { get; set; }
    public long LastUpdated { get; set; }
    public List<AuctionHouseAuctionDto> Auctions { get; set; } = [];
}

internal sealed class AuctionHouseAuctionDto
{
    public ItemDto? Item { get; set; }
}