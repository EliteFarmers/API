using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Admin.Endpoints;

internal sealed class RefreshDiscordGuildEndpoint(
	IDiscordService discordService)
	: Endpoint<GuildIdRequest>
{
	public override void Configure() {
		Post("/admin/guild/{GuildId}/refresh");
		Policies(ApiUserPolicies.Moderator);
		Version(0);

		Description(s => s.Accepts<GuildIdRequest>());

		Summary(s => {
			s.Summary = "Refresh a guild";
			s.Description = "This fetches the latest data from Discord for the specified guild";
		});
	}

	public override async Task HandleAsync(GuildIdRequest request, CancellationToken c) {
		await discordService.RefreshDiscordGuild(request.GuildIdUlong, true);
		await Send.NoContentAsync(c);
	}
}