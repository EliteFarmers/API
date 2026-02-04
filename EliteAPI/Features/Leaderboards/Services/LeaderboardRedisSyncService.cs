using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Features.Leaderboards.Services;

public class LeaderboardRedisSyncService(
    IServiceProvider serviceProvider,
    IConnectionMultiplexer redis,
    ILogger<LeaderboardRedisSyncService> logger,
    IOptions<ConfigLeaderboardSettings> settings) : BackgroundService
{
    private readonly ConfigLeaderboardSettings _settings = settings.Value;
    private static readonly RedisValue ProfileHash = "p";
    private static readonly RedisValue IgnHash = "i";
    private static readonly RedisValue UuidHash = "u";
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public async Task ForceUpdateAsync(CancellationToken ct) {
        await _syncLock.WaitAsync(ct);
        try {
            await UpdateRequestedLeaderboards(ct);
        }
        finally {
            _syncLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Leaderboard Redis Sync Service starting (On-Demand Mode).");

        // Wait a bit for startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        var interval = _settings.CompleteRefreshInterval > 0 
            ? TimeSpan.FromMinutes(_settings.CompleteRefreshInterval) 
            : TimeSpan.FromMinutes(5); // Smaller interval for smarter sync

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _syncLock.WaitAsync(stoppingToken);
                try {
                    await UpdateRequestedLeaderboards(stoppingToken);
                }
                finally {
                    _syncLock.Release();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating leaderboards in background");
            }
        }
    }

    private async Task UpdateRequestedLeaderboards(CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints()[0]);
        
        // Find all requested leaderboards
        var requestKeys = server.Keys(pattern: "lb-req:*").ToList();
        if (requestKeys.Count == 0) return;

        logger.LogInformation("Syncing {Count} requested leaderboards...", requestKeys.Count);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var registrationService = scope.ServiceProvider.GetRequiredService<ILeaderboardRegistrationService>();

        foreach (var reqKey in requestKeys)
        {
            if (ct.IsCancellationRequested) break;

            var parts = reqKey.ToString().Split(':');
            if (parts.Length < 3) continue;

            var slug = parts[1];
            var gameMode = parts[2] == "all" ? null : parts[2];

            try
            {
                if (!registrationService.LeaderboardsById.TryGetValue(slug, out var definition)) continue;
                var lb = await context.Leaderboards.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
                if (lb == null) continue;

                var query = context.LeaderboardEntries.AsNoTracking()
                    .Where(e => e.LeaderboardId == lb.LeaderboardId && !e.IsRemoved && e.IntervalIdentifier == null);

                if (gameMode != null)
                {
                    query = query.Where(e => e.ProfileType == gameMode);
                }

                var entries = await query
                    .Include(e => e.ProfileMember)
                    .ThenInclude(pm => pm.MinecraftAccount)
                    .Include(e => e.ProfileMember)
                    .ThenInclude(pm => pm.Profile)
                    .Include(e => e.Profile) 
                    .ToListAsync(ct); 

                // Use transaction to swap sorted set atomically
                var lbKey = $"lb:{slug}:{parts[2]}";
                var tempKey = $"lb:{slug}:{parts[2]}:temp";

                // Prepare SortedSet entries
                var sortedSetEntries = entries.Select(e => new SortedSetEntry(
                    e.ProfileMemberId?.ToString() ?? e.ProfileId, 
                    (double)e.Score
                )).ToArray();

                if (sortedSetEntries.Length > 0)
                {
                    var transaction = db.CreateTransaction();
                    
                    // Cleanup and setup temp
                    _ = transaction.KeyDeleteAsync(tempKey);
                    _ = transaction.SortedSetAddAsync(tempKey, sortedSetEntries);
                    
                    // Rename temp to real (Atomic swap)
                    _ = transaction.KeyRenameAsync(tempKey, lbKey, When.Always);
                    
                    // Set TTL on data (it should expire if no longer requested)
                    _ = transaction.KeyExpireAsync(lbKey, TimeSpan.FromHours(1));

                    await transaction.ExecuteAsync();

                    // Update member metadata (can be fire-and-forget or batch)
                    var batch = db.CreateBatch();
                    foreach (var e in entries)
                    {
                       var memberId = e.ProfileMemberId?.ToString() ?? e.ProfileId;
                       var memberKey = $"member:{memberId}";
                       
                       string ign = "", uuid = "", profileName = "";
                       if (e.ProfileMember != null)
                       {
                           ign = e.ProfileMember.MinecraftAccount.Name;
                           uuid = e.ProfileMember.PlayerUuid;
                           profileName = e.ProfileMember.ProfileName ?? e.ProfileMember.Profile.ProfileName;
                       }
                       else if (e.Profile != null)
                       {
                           profileName = e.Profile.ProfileName;
                           uuid = e.Profile.ProfileId; 
                       }

                       _ = batch.HashSetAsync(memberKey, new HashEntry[]
                       {
                           new(ProfileHash, profileName),
                           new(IgnHash, ign),
                           new(UuidHash, uuid)
                       });
                       _ = batch.KeyExpireAsync(memberKey, TimeSpan.FromMinutes(60));
                    }
                    batch.Execute();

                    logger.LogInformation("Synced {Slug} ({Mode}) to Redis ({Count} entries).", slug, parts[2], entries.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update requested leaderboard {Slug}:{Mode}", slug, parts[2]);
            }
        }
    }
}
