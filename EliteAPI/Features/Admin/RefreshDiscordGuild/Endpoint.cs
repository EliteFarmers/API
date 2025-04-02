using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Admin.RefreshDiscordGuild;

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

	public override async Task HandleAsync(GuildIdRequest request, CancellationToken c) 
	{
		await discordService.RefreshDiscordGuild(request.GuildIdUlong, replaceImages: true);
		await SendNoContentAsync(cancellation: c);
	}
}