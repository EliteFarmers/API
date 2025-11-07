using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class AuctionOverviewResponse
{
	public List<EndedAuctionDto> Ended { get; set; } = [];
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
		var data = await context.EndedAuctions.AsNoTracking()
			.OrderByDescending(a => a.Timestamp)
			.Take(10)
			.ToListAsync(c);
		
		var uuids = data.Select(d => d.BuyerUuid.ToString("N"))
			.Concat(data.Select(d => d.SellerUuid.ToString("N")))
			.Distinct()
			.ToList();

		var meta = await accountService.GetAccountMeta(uuids);

		var response = new AuctionOverviewResponse {
			Ended = data.Select(e => {
				var dto = e.ToDto();
				dto.Buyer = meta.GetValueOrDefault(dto.BuyerUuid.ToString("N"));
				dto.Seller = meta.GetValueOrDefault(dto.SellerUuid.ToString("N"));
				return dto;
			}).ToList()
		};

		await Send.OkAsync(response, c);
	}
}