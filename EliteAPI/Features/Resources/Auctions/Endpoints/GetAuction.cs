using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

public class GetAuctionRequest
{
    public required Guid AuctionId { get; set; }
}

public class GetAuctionResponse
{
    public required AuctionDto Auction { get; set; }
}

public class GetAuction(DataContext context) : Endpoint<GetAuctionRequest, GetAuctionResponse>
{

    public override void Configure()
    {
        Get("/resources/auctions/{AuctionId}");
        AllowAnonymous();
        
        Summary(s => {
            s.Summary = "Get Auction";
            s.Description = "Get a specific auction by id. Will not fetch auction from Hypixel, only returns auctions that exist locally.";
        });
    }

    public override async Task HandleAsync(GetAuctionRequest r, CancellationToken c)
    {
        var auction = context.Auctions.FirstOrDefault(e => e.AuctionId == r.AuctionId);

        if (auction is null) {
            await Send.NotFoundAsync(c);
            return;
        }

        await Send.OkAsync(new GetAuctionResponse() {
            Auction = auction.ToDto(),
        }, c);
    }
}