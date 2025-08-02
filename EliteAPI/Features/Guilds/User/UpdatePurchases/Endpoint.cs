using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.UpdatePurchases;

internal sealed class UpdateGuildPurchasesEndpoint(
	IMonetizationService monetizationService,
	IDiscordService discordService,
	DataContext context
) : Endpoint<DiscordIdRequest> {
	
	public override void Configure() {
		Post("/user/guild/{DiscordId}/purchases");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		
		Summary(s => {
			s.Summary = "Refresh Guild Purchases";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		await monetizationService.FetchGuildEntitlementsAsync(request.DiscordIdUlong);
		if (guild.Features.Locked) {
			ThrowError("Guild is locked", StatusCodes.Status400BadRequest);
		}
        
		var entitlements = await monetizationService.GetEntitlementsAsync(request.DiscordIdUlong);
		if (entitlements is { Count: 0 }) {
			await SendNoContentAsync(cancellation: c);
			return;
		}
        
		var maxLeaderboards = guild.Features.JacobLeaderboard?.MaxLeaderboards ?? 0;
		var maxEvents = guild.Features.EventSettings?.MaxMonthlyEvents ?? 0;

		var currentLeaderboards = 0;
		var currentEvents = 0;

		foreach (var entitlement in entitlements) {
			if (!entitlement.IsActive) continue;

			var features = entitlement.Product.Features;
			if (features is { MaxMonthlyEvents: > 0 }) {
				currentEvents = Math.Max(features.MaxMonthlyEvents.Value, currentEvents);
			}
            
			if (features is { MaxJacobLeaderboards: > 0 }) {
				currentLeaderboards = Math.Max(features.MaxJacobLeaderboards.Value, currentLeaderboards);
			}
		}

		if (currentLeaderboards == maxLeaderboards && currentEvents == maxEvents) {
			await SendNoContentAsync(cancellation: c);
			return;
		}
        
		guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
		guild.Features.JacobLeaderboard.MaxLeaderboards = currentLeaderboards;
		guild.Features.EventSettings ??= new GuildEventSettings();
		guild.Features.EventSettings.MaxMonthlyEvents = currentEvents;
            
		context.Entry(guild).Property(p => p.Features).IsModified = true;
		context.Guilds.Update(guild);
        
		await context.SaveChangesAsync(c);
		
		await SendNoContentAsync(cancellation: c);
	}
}