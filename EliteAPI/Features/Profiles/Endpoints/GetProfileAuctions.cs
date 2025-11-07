using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Textures.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Profiles.Endpoints;

public class GetProfileAuctionsRequest : PlayerUuidRequest
{
	public required string ProfileUuid { get; set; }

	[JsonIgnore] public string ProfileUuidFormatted => ProfileUuid.ToLowerInvariant().Replace("-", "");
}

public class GetProfileAuctionsResponse
{
	/// <summary>
	/// Ended auctions
	/// </summary>
	public List<EndedAuctionDto> Ended { get; set; } = [];
}

internal sealed class GetProfileAuctionsEndpoint(
	IMemberService memberService,
	DataContext context,
	ItemTextureResolver itemTextureResolver
) : Endpoint<GetProfileAuctionsRequest, GetProfileAuctionsResponse>
{
	public override void Configure() {
		Get("/profile/{PlayerUuid}/{ProfileUuid}/auctions");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Specific Profile Member Auctions"; });
	}

	public override async Task HandleAsync(GetProfileAuctionsRequest request, CancellationToken c) {
		var memberId = await memberService.GetProfileMemberId(request.PlayerUuidFormatted, request.ProfileUuidFormatted);
		if (memberId is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var endedAuctions = await context.EndedAuctions
			.Where(a => a.SellerProfileMemberId == memberId)
			.OrderByDescending(a => a.Timestamp)
			.Take(50)
			.ToListAsync(c);

		var result = new GetProfileAuctionsResponse {
			Ended = endedAuctions.Select(e => e.ToDto()).ToList(),
		};
		
		await Send.OkAsync(result, c);
	}
}

internal sealed class GetProfileAuctionsRequestValidator : Validator<GetProfileAuctionsRequest>
{
	public GetProfileAuctionsRequestValidator() {
		Include(new PlayerUuidRequestValidator());
		RuleFor(x => x.ProfileUuid)
			.NotEmpty()
			.WithMessage("PlayerUuid is required")
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$");
	}
}