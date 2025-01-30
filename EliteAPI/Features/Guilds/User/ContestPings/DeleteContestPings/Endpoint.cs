using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.ContestPings.DeleteContestPings;

internal sealed class DeleteContestPingsEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<DisableContestPingsRequest> {
	
	public override void Configure() {
		Delete("/user/guild/{DiscordId}/contestpings");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Delete contest pings for a guild";
		});
		
	}

	public override async Task HandleAsync(DisableContestPingsRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		if (!guild.HasBot) {
			if (guild?.Features.ContestPings?.Enabled is true) {
				guild.Features.ContestPings.Enabled = false;
				guild.Features.ContestPings.DisabledReason = "Guild no longer found.";
			}
            
			ThrowError("Guild no longer has the bot", StatusCodes.Status400BadRequest);
		}

		if (!guild.Features.ContestPingsEnabled) {
			await SendNoContentAsync(c);
			return;
		}

		var pings = guild.Features.ContestPings ?? new ContestPingsFeature();

		pings.Enabled = false;
		pings.DisabledReason = request.Reason;
        
		guild.Features.ContestPings = pings;
		context.Entry(guild).Property(g => g.Features).IsModified = true;
		
		await context.SaveChangesAsync(c);
		await SendNoContentAsync(c);
	}
}