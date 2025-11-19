using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class ItemEndedAuctionsRequest
{
    public string SkyblockId { get; set; } = string.Empty;
    
    [QueryParam]
    public string? Variant { get; set; }
    
    [QueryParam]
    public int Limit { get; set; } = 10;
}

internal sealed class GetItemEndedAuctionsEndpoint(
    DataContext context,
    IAccountService accountService,
    VariantBundleService bundleService
) : Endpoint<ItemEndedAuctionsRequest, List<EndedAuctionDto>>
{
    public override void Configure()
    {
        Get("/resources/auctions/{SkyblockId}/ended");
        AllowAnonymous();
        Version(0);
        
        Summary(s => {
            s.Summary = "Get Recently Ended Auctions for Item";
            s.Description = "Get recently ended auctions for a specific item and optional variant. Supports bundle: format for pets and runes.";
        });
        
        ResponseCache(60);
    }

    public override async Task HandleAsync(ItemEndedAuctionsRequest req, CancellationToken c)
    {
        var bundle = bundleService.ParseBundleKey(req.SkyblockId);
        
        IQueryable<EndedAuction> query;
        
        if (bundle is null)
        {
            query = context.EndedAuctions.AsNoTracking()
                .Where(a => a.SkyblockId == req.SkyblockId);

            if (!string.IsNullOrEmpty(req.Variant))
            {
                query = query.Where(a => a.VariantKey == req.Variant);
            }
        }
        else
        {
            if (!bundleService.IsValidBundleId(bundle.Value.SkyblockId))
            {
                await Send.NotFoundAsync(c);
                return;
            }
            
            query = context.EndedAuctions.AsNoTracking()
                .Where(a => a.SkyblockId == bundle.Value.SkyblockId);
        }

        var data = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(Math.Clamp(req.Limit, 1, 100) * (bundle is null ? 1 : 10))
            .ToListAsync(c);

        if (bundle is not null)
        {
            data = data
                .Where(a => bundleService.MatchesVariantBundle(a.VariantKey, bundle.Value))
                .Take(Math.Clamp(req.Limit, 1, 100))
                .ToList();
        }

        var uuids = data.Select(d => d.BuyerUuid.ToString("N"))
            .Concat(data.Select(d => d.SellerUuid.ToString("N")))
            .Distinct()
            .ToList();

        var meta = await accountService.GetAccountMeta(uuids);

        var response = data.Select(e => {
            var dto = e.ToDto();
            dto.Buyer = meta.GetValueOrDefault(dto.BuyerUuid.ToString("N"));
            dto.Seller = meta.GetValueOrDefault(dto.SellerUuid.ToString("N"));
            return dto;
        }).ToList();

        await Send.OkAsync(response, c);
    }
}
