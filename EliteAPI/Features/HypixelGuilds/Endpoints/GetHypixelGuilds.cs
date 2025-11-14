using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Features.HypixelGuilds.Services;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

internal sealed class GetHypixelGuildsRequest
{
	[QueryParam]
	public SortHypixelGuildsBy SortBy { get; set; } = SortHypixelGuildsBy.SkyblockExperienceAverage;
	[QueryParam]
	public bool Descending { get; set; } = true;
	[QueryParam]
	public int Page { get; set; } = 1;
	[QueryParam]
	public int PageSize { get; set; } = 50;
}

internal sealed class GetHypixelGuildsResponse
{
	public required List<HypixelGuildDetailsDto> Guilds { get; set; }
}

internal sealed class GetHypixelGuildsEndpoint(IHypixelGuildService hypixelGuildService) : Endpoint<GetHypixelGuildsRequest, GetHypixelGuildsResponse>
{
	public override void Configure() {
		Get("/hguilds");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Hypixel Guilds"; });
		
		Options(o => {
			o.AutoTagOverride("Hypixel Guilds");
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(10)).Tag("hypixel-guilds"));
		});
	}

	public override async Task HandleAsync(GetHypixelGuildsRequest request, CancellationToken c) {
		var result = await hypixelGuildService.GetGuildListAsync(new HypixelGuildListQuery {
			SortBy = request.SortBy,
			Descending = request.Descending,
			Page = request.Page,
			PageSize = request.PageSize,
		}, c);
		
		await Send.OkAsync(new GetHypixelGuildsResponse() {
			Guilds = result
		}, c);
	}
}