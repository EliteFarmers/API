using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetJacobLeaderboards;

internal sealed class SetJacobFeatureEndpoint(
	IDiscordService discordService,
	DataContext context)
	: Endpoint<SetJacobFeatureRequest>
{
	public override void Configure() {
		Post("/guild/{DiscordId}/jacob");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Modify guild jacob permissions";
		});
	}

	public override async Task HandleAsync(SetJacobFeatureRequest request, CancellationToken c) 
	{
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}

		guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
		guild.Features.JacobLeaderboard.MaxLeaderboards = request.Max ?? guild.Features.JacobLeaderboard.MaxLeaderboards;
		guild.Features.JacobLeaderboardEnabled = request.Enable ?? guild.Features.JacobLeaderboardEnabled;

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		
		await SendOkAsync(cancellation: c);
	}
}