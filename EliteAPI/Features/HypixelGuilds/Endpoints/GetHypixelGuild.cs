using EliteAPI.Data;
using EliteAPI.Features.HypixelGuilds.Models;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

internal sealed class GetHypixelGuildRequest
{
	public string GuildId { get; set; }
}

internal sealed class GetHypixelGuildResponse
{
	public required HypixelGuildDto Guild { get; set; }
}


internal sealed class GetHypixelGuildEndpoint(DataContext context) : Endpoint<GetHypixelGuildRequest, GetHypixelGuildResponse>
{
	public override void Configure() {
		Get("/hguilds/{GuildId}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Hypixel Guild"; });
		
		Options(o => {
			o.AutoTagOverride("Hypixel Guilds");
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(10)).Tag("hypixel-guilds"));
		});
	}

	public override async Task HandleAsync(GetHypixelGuildRequest request, CancellationToken c) {
		var oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7);
		var date = new DateOnly(oneWeekAgo.Year, oneWeekAgo.Month, oneWeekAgo.Day);
		
		var guild = await context.HypixelGuilds
			.Include(g => g.Members.Where(m => m.Active))
			.ThenInclude(g => g.ExpHistory.Where(e => e.Day >= date))
			.Include(g => g.Members.Where(m => m.Active))
			.ThenInclude(g => g.MinecraftAccount)
			.ThenInclude(g => g.EliteAccount)
			.ThenInclude(g => g!.UserSettings)
			.Include(g => g.Stats.OrderByDescending(s => s.RecordedAt).Take(1))
			.FirstOrDefaultAsync(g => g.Id == request.GuildId, cancellationToken: c);

		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		};
		
		await Send.OkAsync(new GetHypixelGuildResponse() {
			Guild = guild.ToDto(),
		}, c);
	}
}