using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

public class GetAuctionVariantsRequest
{
    public required string SkyblockId { get; set; }
}

public class GetAuctionVariantsResponse
{
    public required List<AuctionItemDto> Variants { get; set; }
}

internal sealed class GetAuctionVariantsEndpoint(DataContext context) : Endpoint<GetAuctionVariantsRequest, GetAuctionVariantsResponse>
{
    public override void Configure()
    {
        Get("/resources/auctions/{SkyblockId}/variants");
        AllowAnonymous();
        Version(0);

        Summary(s =>
        {
            s.Summary = "List Auction Variants";
            s.Description = "Retrieve aggregated BIN statistics for each tracked variant of a SkyBlock item.";
        });
    }

    public override async Task HandleAsync(GetAuctionVariantsRequest req, CancellationToken ct)
    {
        var variants = await context.AuctionItems
            .AsNoTracking()
            .Where(a => a.SkyblockId == req.SkyblockId)
            .OrderBy(a => a.VariantKey)
            .ToListAsync(ct);

        if (variants.Count == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var response = new GetAuctionVariantsResponse
        {
            Variants = variants.Select(v => v.ToDto()).ToList()
        };

        await Send.OkAsync(response, ct);
    }
}
