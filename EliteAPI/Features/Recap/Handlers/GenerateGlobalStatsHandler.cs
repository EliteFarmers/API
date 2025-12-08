using EliteAPI.Features.Recap.Commands;
using EliteAPI.Features.Recap.Services;
using FastEndpoints;
using Microsoft.Extensions.Caching.Hybrid;

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
                throw;
            }
        }, new HybridCacheEntryOptions() {
            LocalCacheExpiration = TimeSpan.FromMinutes(30),
        }, cancellationToken: ct);
    }
}
