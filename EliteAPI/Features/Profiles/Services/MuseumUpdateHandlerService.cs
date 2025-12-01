using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Caching.Hybrid;

namespace EliteAPI.Features.Profiles.Services;

public class MuseumUpdateCommand : ICommand
{
	public required string ProfileId { get; set; }
}

public class MuseumUpdateHandlerService(
	IServiceScopeFactory scopeFactory
) : ICommandHandler<MuseumUpdateCommand>
{
	public async Task ExecuteAsync(MuseumUpdateCommand command, CancellationToken ct) {
		using var scope = scopeFactory.CreateScope();
		
		var museumService = scope.ServiceProvider.GetRequiredService<IMuseumService>();
		var cache = scope.ServiceProvider.GetRequiredService<HybridCache>();
		
		// Use hybrid cache to prevent race conditions causing this to update twice in a short period
		var cacheKey = $"MuseumUpdateService_{command.ProfileId}";
		await cache.GetOrCreateAsync(cacheKey, async (c) => {
			await museumService.UpdateMuseum(command.ProfileId, c);
			return true;
		}, new HybridCacheEntryOptions() {
			LocalCacheExpiration = TimeSpan.FromMinutes(1),
		}, cancellationToken: ct);
	}
}