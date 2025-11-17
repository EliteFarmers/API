using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.HypixelGuilds.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Endpoints;

internal sealed class HypixelGuildRefreshRequest
{
	public required string GuildId { get; set; }
}

internal sealed class RefreshHypixelGuildEndpoint(
	DataContext context,
	IHypixelGuildService service)
	: Endpoint<HypixelGuildRefreshRequest>
{
	public override void Configure() {
		Post("/admin/hguilds/{GuildId}/refresh");
		Policies(ApiUserPolicies.Moderator);
		Version(0);

		Description(s => {
			s.Accepts<HypixelGuildRefreshRequest>();
		});

		Summary(s => {
			s.Summary = "Refresh a Hypixel Guild";
			s.Description = "This fetches the latest data from Hypixel for the specified guild";
		});
	}

	public override async Task HandleAsync(HypixelGuildRefreshRequest request, CancellationToken c) {
		var existingGuild = await context.HypixelGuilds.FirstOrDefaultAsync(g => g.Id == request.GuildId, cancellationToken: c);
		if (existingGuild is not null) {
			existingGuild.LastUpdated = 0;
			await context.SaveChangesAsync(c);
		}
		
		await service.UpdateGuildIfNeeded(request.GuildId, c);
		
		await Send.NoContentAsync(c);
	}
}