using FastEndpoints;
using Microsoft.Extensions.Caching.Hybrid;

namespace EliteAPI.Features.HypixelGuilds.Services;

public class HypixelGuildStatUpdateCommand : ICommand
{
	public string GuildId { get; set; }
}

public class HypixelGuildStatHandler(
	IServiceScopeFactory scopeFactory
	) : ICommandHandler<HypixelGuildStatUpdateCommand>
{
	public async Task ExecuteAsync(HypixelGuildStatUpdateCommand command, CancellationToken ct) {
		using var scope = scopeFactory.CreateScope();
		
		var statsService = scope.ServiceProvider.GetRequiredService<IHypixelGuildStatsService>();
		var cache = scope.ServiceProvider.GetRequiredService<HybridCache>();
		
		// Use hybrid cache to prevent race conditions causing this to update twice in a short period
		var cacheKey = $"HypixelGuildStatUpdate_{command.GuildId}";
		await cache.GetOrCreateAsync(cacheKey, async (c) => {
			await statsService.UpdateGuildStats(command.GuildId, c);
			return true;
		}, new HybridCacheEntryOptions() {
			LocalCacheExpiration = TimeSpan.FromMinutes(1),
		}, cancellationToken: ct);
	}
}