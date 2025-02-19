using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.ContestPings.UpdateContestPings;

internal sealed class DeleteContestPingsEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<UpdateContestPingsRequest> {
	
	public override void Configure() {
		Put("/user/guild/{DiscordId}/contestpings");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Update contest pings for a guild";
		});
	}

	public override async Task HandleAsync(UpdateContestPingsRequest request, CancellationToken c) {
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
			ThrowError("Contest pings are not enabled for this guild", StatusCodes.Status400BadRequest);
		}

		var pings = guild.Features.ContestPings ?? new ContestPingsFeature();

		pings.Enabled = request.Enabled;
		pings.ChannelId = request.ChannelId;
		pings.DelaySeconds = request.DelaySeconds;
		pings.AlwaysPingRole = request.AlwaysPingRole;
		pings.CropPingRoles = request.CropPingRoles;

		if (pings is { Enabled: true, DisabledReason: not null }) {
			pings.DisabledReason = null;
		} 
        
		guild.Features.ContestPings = pings;
		context.Entry(guild).Property(g => g.Features).IsModified = true;

		await context.SaveChangesAsync(c);
		await SendNoContentAsync(cancellation: c);
	}
}