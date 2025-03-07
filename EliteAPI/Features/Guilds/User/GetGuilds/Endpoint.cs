using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.GetGuilds;

internal sealed class GetUserGuildsEndpoint(
	IDiscordService discordService
) : EndpointWithoutRequest<List<GuildMemberDto>> {
	
	public override void Configure() {
		Get("/user/guilds");
		Version(0);

		Summary(s => {
			s.Summary = "Get guild memberships for the current user";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			ThrowError("User not found", StatusCodes.Status404NotFound);
		}

		var result = await discordService.GetUsersGuilds(userId);
		await SendAsync(result, cancellation: c);
	}
}