using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetEvents;

internal sealed class SetEventFeatureEndpoint(
	IDiscordService discordService,
	DataContext context)
	: Endpoint<SetEventFeatureRequest>
{
	public override void Configure() {
		Post("/guild/{DiscordId}/events");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Description(x => x.Accepts<SetEventFeatureRequest>());
		
		Summary(s => {
			s.Summary = "Modify guild event permissions";
		});
	}

	public override async Task HandleAsync(SetEventFeatureRequest request, CancellationToken c) 
	{
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}

		guild.Features.EventSettings ??= new GuildEventSettings();
		guild.Features.EventSettings.MaxMonthlyEvents = request.Max ?? guild.Features.EventSettings.MaxMonthlyEvents;
		guild.Features.EventsEnabled = request.Enable ?? guild.Features.EventsEnabled;

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		
		await SendNoContentAsync(cancellation: c);
	}
}