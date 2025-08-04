using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Admin.ClearPlayerCooldowns;

internal sealed class ClearPlayerCooldownsEndpoint(
	DataContext context,
	IConnectionMultiplexer redis,
	IMojangService mojangService)
	: Endpoint<PlayerRequest> 
{
	public override void Configure() {
		Post("/admin/cooldowns/player/{Player}");
		Policies(ApiUserPolicies.Moderator);
		Version(0);
		
		Description(x => x.Accepts<PlayerRequest>());
		
		Summary(s => {
			s.Summary = "Reset a player's cooldowns";
			s.Description = "This enables a player's data from Hypixel to be refreshed on the next request.";
		});
	}

	public override async Task HandleAsync(PlayerRequest request, CancellationToken c) 
	{
		var account = await mojangService.GetMinecraftAccountByUuidOrIgn(request.Player);
		if (account is null) {
			await Send.NotFoundAsync(cancellation: c);
			return;
		}

		account.LastUpdated = 0;
		account.ProfilesLastUpdated = 0;
		account.PlayerDataLastUpdated = 0;
        
		if (context.Entry(account).State != EntityState.Modified) {
			context.Entry(account).State = EntityState.Modified;
		}
        
		await context.SaveChangesAsync(c);
        
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync($"player:{account.Id}:updating");
		await db.KeyDeleteAsync($"profile:{account.Id}:updating");

		await Send.NoContentAsync(cancellation: c);
	}
}