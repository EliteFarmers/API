using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Bot.DisableContestPings;

internal sealed class DisableContestPingsPingsEndpoint(
	DataContext context
) : Endpoint<DisableContestPingsRequest> {
	
	public override void Configure() {
		Delete("/bot/contestpings/{DiscordId}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);
		
		Description(x => x.Accepts<DisableContestPingsRequest>());

		Summary(s => {
			s.Summary = "Disable contest pings for a guild";
		});
	}

	public override async Task HandleAsync(DisableContestPingsRequest request, CancellationToken c) {
		var guild = await context.Guilds.FirstOrDefaultAsync(g => g.Id == request.DiscordIdUlong, c);
		if (guild?.Features.ContestPings is null) {
			await SendNotFoundAsync(c);
			return;
		}

		var pings = guild.Features.ContestPings ?? new ContestPingsFeature();

		pings.Enabled = false;
		pings.DisabledReason = request.Reason;
        
		guild.Features.ContestPings = pings;
		context.Entry(guild).Property(g => g.Features).IsModified = true;

		await context.SaveChangesAsync(c);
		
		await SendNoContentAsync(cancellation: c);
	}
}