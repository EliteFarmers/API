using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Bot.GetContestPings;

internal sealed class GetContestPingsEndpoint(
	DataContext context
) : EndpointWithoutRequest<List<ContestPingsFeatureDto>> {
	
	public override void Configure() {
		Get("/bot/contestpings");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => {
			s.Summary = "Get list of guilds with contest pings enabled";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var guilds = await context.Guilds
			.Where(g => g.Features.ContestPingsEnabled == true
			            && g.Features.ContestPings != null
			            && g.Features.ContestPings.Enabled)
			.ToListAsync(cancellationToken: c);

		var result = guilds
			.Where(g => !string.IsNullOrWhiteSpace(g.Features.ContestPings!.ChannelId))
			.Select(g => new ContestPingsFeatureDto {
				GuildId = g.Id.ToString(),
				ChannelId = g.Features.ContestPings?.ChannelId ?? string.Empty,
				AlwaysPingRole = g.Features.ContestPings?.AlwaysPingRole ?? string.Empty,
				CropPingRoles = g.Features.ContestPings?.CropPingRoles ?? new CropSettings<string>(),
				DelaySeconds = g.Features.ContestPings?.DelaySeconds ?? 0,
				DisabledReason = g.Features.ContestPings?.DisabledReason ?? string.Empty,
				Enabled = g.Features.ContestPings?.Enabled ?? false
			}).ToList();
		
		await SendAsync(result, cancellation: c);
	}
}