using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Guilds.Services;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Guilds.User.Jacob.SendJacobLeaderboard;

internal sealed class SendGuildJacobFeatureEndpoint(
	IDiscordService discordService,
	IGuildService guildService
) : Endpoint<SendJacobLeaderboardRequest> {
	
	public override void Configure() {
		Post("/user/guild/{DiscordId}/jacob/{LeaderboardId}/send");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		
		Description(x => x.Accepts<SendJacobLeaderboardRequest>());

		Summary(s => {
			s.Summary = "Send a Jacob leaderboard to Discord";
		});
	}

	public override async Task HandleAsync(SendJacobLeaderboardRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var feature = guild.Features.JacobLeaderboard;
		var existing = feature.Leaderboards.FirstOrDefault(lb => lb.Id.Equals(request.LeaderboardId));
        
		if (existing is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		if (existing.ChannelId is null) {
			ThrowError("ChannelId is null", StatusCodes.Status400BadRequest);
		}

		feature.Leaderboards.Remove(existing);

		var author = User.GetId();
		var result = await guildService.SendLeaderboardPanel(request.DiscordIdUlong, existing.ChannelId, author ?? "", request.LeaderboardId);
		
		if (result is NotFoundObjectResult) {
			await Send.NotFoundAsync(c);
			return;
		}

		await Send.NoContentAsync(cancellation: c);
	}
}