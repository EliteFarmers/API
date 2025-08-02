using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Guilds.User.Jacob.UpdateJacob;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Bot.UpdateJacobFeature;

internal sealed class UpdateJacobFeatureEndpoint(
	DataContext context,
	IDiscordService discordService
) : Endpoint<UpdateJacobFeatureRequest, GuildJacobLeaderboardFeature> {

	public override void Configure() {
		Put("/bot/{DiscordId}/jacob");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => { s.Summary = "Update guild jacob feature"; });
	}

	public override async Task HandleAsync(UpdateJacobFeatureRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}

		if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
			await SendNotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;

		feature.BlockedRoles = request.Feature.BlockedRoles;
		feature.BlockedUsers = request.Feature.BlockedUsers;
		feature.RequiredRoles = request.Feature.RequiredRoles;
		feature.ExcludedParticipations = request.Feature.ExcludedParticipations;
		feature.ExcludedTimespans = request.Feature.ExcludedTimespans;
		feature.Leaderboards = request.Feature.Leaderboards;

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendNoContentAsync(cancellation: c);
	}
}