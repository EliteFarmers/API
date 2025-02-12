using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Guilds.User.Jacob.UpdateJacob;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.UpdateJacobFeature;

internal sealed class UpdateJacobFeatureEndpoint(
	DataContext context,
	IDiscordService discordService
) : Endpoint<UpdateJacobFeatureRequest, GuildJacobLeaderboardFeature> {

	public override void Configure() {
		Put("/bot/{DiscordId}/jacob");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
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

		feature.BlockedRoles = request.BlockedRoles;
		feature.BlockedUsers = request.BlockedUsers;
		feature.RequiredRoles = request.RequiredRoles;
		feature.ExcludedParticipations = request.ExcludedParticipations;
		feature.ExcludedTimespans = request.ExcludedTimespans;

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendOkAsync(cancellation: c);
	}
}