using EliteAPI.Features.Recap.Commands;
using EliteAPI.Features.Recap.Services;
using FastEndpoints;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EliteAPI.Features.Recap.Handlers;

public class GenerateGlobalStatsHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<GenerateGlobalStatsHandler> logger
    ) : ICommandHandler<GenerateGlobalStatsCommand>
{
    public async Task ExecuteAsync(GenerateGlobalStatsCommand command, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        
        var recapService = scope.ServiceProvider.GetRequiredService<IYearlyRecapService>();
        var cache = scope.ServiceProvider.GetRequiredService<HybridCache>();
        
        // Use hybrid cache to prevent race conditions causing this to update twice in a short period
        // The cache key ensures that if multiple commands are queued, only one effectively runs the logic while others wait or return cached 'true'
        var cacheKey = $"GlobalRecapGeneration_{command.Year}";
        
        await cache.GetOrCreateAsync(cacheKey, async (c) => {
            logger.LogInformation("Starting Global Stats generation for year {Year}", command.Year);
            try 
            {
                await recapService.GenerateGlobalStatsAsync(command.Year);
                logger.LogInformation("Completed Global Stats generation for year {Year}", command.Year);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate global stats for {Year}", command.Year);
                throw; // Throwing will clean up the cache entry so it can be retried
            }
        }, new HybridCacheEntryOptions() {
            LocalCacheExpiration = TimeSpan.FromMinutes(5), // Keep the "Processing/Done" flag for a bit
        }, cancellationToken: ct);
    }
}
