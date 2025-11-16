using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Features.HypixelGuilds.Services;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

internal sealed class SearchHypixelGuildsRequest
{
	[QueryParam]
	public required string Query { get; set; }

	[QueryParam]
	public int Limit { get; set; } = 10;
}

internal sealed class SearchHypixelGuildsResponse
{
	public required IReadOnlyList<HypixelGuildSearchResultDto> Results { get; set; }
}

internal sealed class SearchHypixelGuildsEndpoint(IHypixelGuildService guildService)
	: Endpoint<SearchHypixelGuildsRequest, SearchHypixelGuildsResponse>
{
	public override void Configure() {
		Get("/hguilds/search");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Search Hypixel Guilds";
			s.Description = "Fuzzy search across Hypixel guild names";
		});

		Options(o => {
			o.AutoTagOverride("Hypixel Guilds");
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(1)).Tag("hypixel-guilds-search")
				.SetVaryByQuery(["query", "limit"]));
		});
	}

	public override async Task HandleAsync(SearchHypixelGuildsRequest req, CancellationToken ct) {
		var results = await guildService.SearchGuildsAsync(req.Query, req.Limit, ct);
		await Send.OkAsync(new SearchHypixelGuildsResponse { Results = results }, cancellation: ct);
	}
}

internal sealed class SearchHypixelGuildsRequestValidator : Validator<SearchHypixelGuildsRequest>
{
	public SearchHypixelGuildsRequestValidator() {
		RuleFor(x => x.Query)
			.NotEmpty()
			.MinimumLength(1)
			.MaximumLength(64);

		RuleFor(x => x.Limit)
			.GreaterThan(0)
			.LessThanOrEqualTo(50);
	}
}

