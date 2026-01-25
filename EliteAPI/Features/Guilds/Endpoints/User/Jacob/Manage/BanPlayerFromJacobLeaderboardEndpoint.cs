using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class BanPlayerRequest : JacobManageRequest
{
	[FromBody]
	public required BanPlayerRequestBody Body { get; set; } = null!;
	
	public class BanPlayerRequestBody
	{
		public required string PlayerUuid { get; set; }
	}
}

internal sealed class BanPlayerFromJacobLeaderboardEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<BanPlayerRequest>
{
	public override void Configure() {
		Post("/guilds/{DiscordId}/jacob/bans/players");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		Summary(s => { s.Summary = "Ban a player from all Jacob leaderboards"; });
	}

	public override async Task HandleAsync(BanPlayerRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild?.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;
		
		if (feature.BlockedPlayerUuids.Count >= 100) {
			ThrowError("Player ban limit reached (100).", StatusCodes.Status400BadRequest);
			return;
		}

		if (feature.BlockedPlayerUuids.Contains(request.Body.PlayerUuid)) {
			ThrowError("Player is already banned.", StatusCodes.Status409Conflict);
			return;
		}

		feature.BlockedPlayerUuids.Add(request.Body.PlayerUuid);

		foreach (var lb in feature.Leaderboards) {
			PurgePlayerFromLeaderboard(lb, request.Body.PlayerUuid);
		}

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}

	private static void PurgePlayerFromLeaderboard(GuildJacobLeaderboard lb, string uuid) {
		lb.Crops.Cactus.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Carrot.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.CocoaBeans.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Melon.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Mushroom.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.NetherWart.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Potato.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Pumpkin.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.SugarCane.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Wheat.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Sunflower.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.Moonflower.RemoveAll(e => e.Uuid == uuid);
		lb.Crops.WildRose.RemoveAll(e => e.Uuid == uuid);
	}
}