using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class AuctionOverviewResponse
{
	public List<AuctionDto> New { get; set; } = [];
	public List<AuctionDto> Ended { get; set; } = [];
}

internal sealed class GetAuctionHouseOverviewEndpoint(
	DataContext context,
	IAccountService accountService
) : EndpointWithoutRequest<object> // Type generator doesn't play nicely with ItemDto being cyclic
{

	public override void Configure() {
		Get("/resources/auctions-overview");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Auction House Overview";
			s.Description = "Get overview of auction house.";
		});

		ResponseCache(300);
		Options(o => {
			o.ClearDefaultProduces();
			o.Produces<AuctionOverviewResponse>();
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(10)).Tag("auctions"));
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var newAuctions = await context.Auctions.AsNoTracking()
			.Where(a => a.Bin && a.SoldAt == 0)
			.OrderByDescending(a => a.Start)
			.Take(10)
			.ToListAsync(c);
		
		var ended = await context.Auctions.AsNoTracking()
			.Where(a => a.Bin && a.BuyerUuid != null)
			.OrderByDescending(a => a.SoldAt)
			.Take(10)
			.ToListAsync(c);
		
		var uuids = ended.Select(d => d.BuyerUuid?.ToString("N"))
			.Concat(ended.Select(d => d.SellerUuid.ToString("N")))
			.Concat(newAuctions.Select(d => d.SellerUuid.ToString("N")))
			.Concat(newAuctions.Select(d => d.BuyerUuid?.ToString("N")))
			.Where(u => u != null)
			.Distinct()
			.ToList();

		var meta = await accountService.GetAccountMeta(uuids!);

		var response = new AuctionOverviewResponse {
			Ended = ended.Select(e => {
				var dto = e.ToDto();
				dto.Buyer = e.BuyerUuid != null ? meta.GetValueOrDefault(e.BuyerUuid.Value.ToString("N")) : null;
				dto.Seller = meta.GetValueOrDefault(dto.SellerUuid.ToString("N"));
				return dto;
			}).ToList(),
			New = newAuctions.Select(e => {
				var dto = e.ToDto();
				dto.Buyer = e.BuyerUuid != null ? meta.GetValueOrDefault(e.BuyerUuid.Value.ToString("N")) : null;
				dto.Seller = meta.GetValueOrDefault(dto.SellerUuid.ToString("N"));
				return dto;
			}).ToList()
		};

		await Send.OkAsync(response, c);
	}
}