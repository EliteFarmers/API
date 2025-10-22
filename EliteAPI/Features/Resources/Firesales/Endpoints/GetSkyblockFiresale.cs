using EliteAPI.Data;
using EliteAPI.Features.Resources.Firesales.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Firesales.Endpoints;

internal sealed class SkyblockFiresaleEndpoint(
	DataContext context
) : EndpointWithoutRequest<SkyblockFiresalesResponse>
{
	public override void Configure() {
		Get("/resources/firesales/current");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Current Skyblock Firesale";
			s.Description = "Get the current/upcoming Skyblock firesales.";
		});

		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("firesales")); });
	}

	public override async Task HandleAsync(CancellationToken c) {
		var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		var result = await context.SkyblockFiresales
			.Where(s => s.EndsAt >= currentTime)
			.SelectDto()
			.ToListAsync(c);

		await Send.OkAsync(new SkyblockFiresalesResponse {
			Firesales = result
		}, c);
	}
}

internal sealed class SkyblockFiresalesResponse
{
	public List<SkyblockFiresaleDto> Firesales { get; set; } = [];
}