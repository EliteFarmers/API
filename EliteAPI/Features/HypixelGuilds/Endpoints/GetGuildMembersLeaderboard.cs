using EliteAPI.Features.Leaderboards.Endpoints;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

public class GetHypixelGuildMembersLeaderboardRequest : LeaderboardRequest
{
	/// <summary>
	/// Guild id to fetch members for (discord/hypixel guild id)
	/// </summary>
	public required string GuildId { get; set; }
}

public class GetHypixelGuildMembersLeaderboardResponse
{
	public required string GuildId { get; set; }
	public required List<LeaderboardEntryDto> Entries { get; set; }
}

internal sealed class GetHypixelGuildMembersLeaderboardEndpoint(
	ILbService lbService
) : Endpoint<GetHypixelGuildMembersLeaderboardRequest, GetHypixelGuildMembersLeaderboardResponse>
{
	public override void Configure() {
		Get("/hguilds/{GuildId}/leaderboards/{Leaderboard}");
		AllowAnonymous();
		Version(0);

		Description(d => d.AutoTagOverride("Hypixel Guilds"));
		Summary(s => { s.Summary = "Get Hypixel Guild Members Leaderboard"; });
	}

	public override async Task HandleAsync(GetHypixelGuildMembersLeaderboardRequest request, CancellationToken c) {
		var entries = await lbService.GetGuildMembersLeaderboardEntriesAsync(
			request.GuildId,
			request.Leaderboard,
			request.Interval,
			request.Mode
		);

		await Send.OkAsync(new GetHypixelGuildMembersLeaderboardResponse {
			GuildId = request.GuildId,
			Entries = entries
		}, c);
	}
}

