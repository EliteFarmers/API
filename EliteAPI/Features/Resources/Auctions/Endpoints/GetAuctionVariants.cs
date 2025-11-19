using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.DTOs;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

public class GetAuctionVariantsRequest
{
	public required string SkyblockId { get; set; }
}

public class GetAuctionVariantsResponse
{
	public required List<AuctionItemDto> Variants { get; set; }
}

internal sealed class GetAuctionVariantsEndpoint(
	DataContext context, 
	VariantBundleService bundleService)
	: Endpoint<GetAuctionVariantsRequest, GetAuctionVariantsResponse>
{
	public override void Configure() {
		Get("/resources/auctions/{SkyblockId}/variants");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "List Auction Variants";
			s.Description = "Retrieve aggregated BIN statistics for each tracked variant of a SkyBlock item.";
		});
	}

	public override async Task HandleAsync(GetAuctionVariantsRequest req, CancellationToken ct) {
		var bundle = bundleService.ParseBundleKey(req.SkyblockId);

		List<AuctionItem> variants;

		if (bundle is null) {
			variants = await context.AuctionItems
				.AsNoTracking()
				.Where(a => a.SkyblockId == req.SkyblockId)
				.OrderBy(a => a.VariantKey)
				.ToListAsync(ct);
		}
		else {
			if (!bundleService.IsValidBundleId(bundle.Value.SkyblockId)) {
				await Send.NotFoundAsync(ct);
				return;
			}

			variants = await context.AuctionItems
				.AsNoTracking()
				.Where(a => a.SkyblockId == bundle.Value.SkyblockId)
				.OrderBy(a => a.VariantKey)
				.ToListAsync(ct);

			variants = variants
				.Where(a => bundleService.MatchesVariantBundle(a.VariantKey, bundle.Value))
				.ToList();
		}

		if (variants.Count == 0) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var response = new GetAuctionVariantsResponse {
			Variants = variants.Select(v => v.ToDto()).ToList()
		};

		await Send.OkAsync(response, ct);
	}
}