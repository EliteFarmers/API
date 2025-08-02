using EliteAPI.Features.Account.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.RefreshGuildMemberships;

internal sealed class RefreshGuildMembershipsEndpoint(
	IDiscordService discordService,
	UserManager userManager
) : EndpointWithoutRequest {
	
	public override void Configure() {
		Post("/user/refresh-guilds");
		Version(0);
		
		Description(x => x.ClearDefaultAccepts());

		Summary(s => {
			s.Summary = "Refresh guild memberships for the current user";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var user = await userManager.GetUserAsync(User);
		if (user?.DiscordAccessToken is null) {
			ThrowError("Discord access token not found", StatusCodes.Status401Unauthorized);
		}

		if (user.GuildsLastUpdated.OlderThanSeconds(60)) {
			ThrowError("Guild memberships can only be refreshed once every 60 seconds", StatusCodes.Status429TooManyRequests);
		}
		
		var result = await discordService.FetchUserGuilds(user);
		
		if (result.Count == 0) {
			ThrowError("No guild memberships found", StatusCodes.Status404NotFound);
		}

		await SendNoContentAsync(c);
	}
}