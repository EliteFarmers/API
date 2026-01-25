using System.ComponentModel;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class BanParticipationRequest : JacobManageRequest
{
	[FromBody]
	public required BanParticipationRequestBody Body { get; set; } = null!;
	
	public class BanParticipationRequestBody
	{
		public required string Uuid { get; set; }
		public required string Crop { get; set; }
		public required long Timestamp { get; set; }
	}
}

internal sealed class BanParticipationFromJacobLeaderboardEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<BanParticipationRequest>
{
	public override void Configure() {
		Post("/guilds/{DiscordId}/jacob/bans/participations");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		Summary(s => { s.Summary = "Ban a specific participation from the leaderboard"; });
	}

	public override async Task HandleAsync(BanParticipationRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild?.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;
		var key = $"{request.Body.Timestamp}-{request.Body.Crop}-{request.Body.Uuid}";

		if (feature.ExcludedParticipations.Contains(key)) {
			ThrowError("Participation is already banned.", StatusCodes.Status409Conflict);
			return;
		}

		feature.ExcludedParticipations.Add(key);

		foreach (var lb in feature.Leaderboards) {
			RemoveParticipationFromLeaderboard(lb, request.Body.Uuid, request.Body.Timestamp, request.Body.Crop);
		}

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}

	private static void RemoveParticipationFromLeaderboard(GuildJacobLeaderboard lb, string uuid, long timestamp, string crop) {
		var cropScores = GetCropScoresByCropName(lb.Crops, crop);
		cropScores?.RemoveAll(e => e.Uuid == uuid && e.Record.Timestamp == timestamp);
	}

	private static List<GuildJacobLeaderboardEntry>? GetCropScoresByCropName(CropRecords crops, string crop) {
		return crop.ToLower() switch {
			"cactus" => crops.Cactus,
			"carrot" => crops.Carrot,
			"cocoa beans" or "cocoabeans" => crops.CocoaBeans,
			"melon" => crops.Melon,
			"mushroom" => crops.Mushroom,
			"nether wart" or "netherwart" => crops.NetherWart,
			"potato" => crops.Potato,
			"pumpkin" => crops.Pumpkin,
			"sugar cane" or "sugarcane" => crops.SugarCane,
			"wheat" => crops.Wheat,
			"sunflower" => crops.Sunflower,
			"moonflower" => crops.Moonflower,
			"wild rose" or "wildrose" => crops.WildRose,
			_ => throw new InvalidEnumArgumentException($"{crop} is not a valid crop")
		};
	}
}