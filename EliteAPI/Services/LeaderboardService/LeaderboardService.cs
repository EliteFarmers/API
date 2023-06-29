using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Services.CacheService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services.LeaderboardService; 

public class LeaderboardService : ILeaderboardService {
    
    private readonly ICacheService _cache;
    private readonly DataContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ConfigLeaderboardSettings _settings;

    public LeaderboardService(DataContext dataContext, IConnectionMultiplexer redis, ICacheService cacheService, IOptions<ConfigLeaderboardSettings> lbSettings) {
        _context = dataContext;
        _redis = redis;
        _cache = cacheService;
        _settings = lbSettings.Value;
    }
    
    public async Task<List<LeaderboardEntry<double>>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20) {
        var db = _redis.GetDatabase();
        
        var exists = await db.KeyExistsAsync($"lb:{leaderboardId}");
        if (!exists) await FetchLeaderboard(leaderboardId);
        
        var slice = await db.SortedSetRangeByScoreWithScoresAsync(
            $"lb:{leaderboardId}", 
            order: Order.Descending, 
            skip: offset, 
            take: limit
        );
        
        if (slice.Length == 0) {
            return new List<LeaderboardEntry<double>>();
        }
        
        // Get the hashset for each member
        var tasks = slice.Select(async entry => {
            var memberId = entry.Element.ToString();
            var member = await db.HashGetAllAsync(memberId);
            return new LeaderboardEntry<double> {
                MemberId = memberId,
                Profile = member.FirstOrDefault(x => x.Name == "profile").Value,
                Ign = member.FirstOrDefault(x => x.Name == "ign").Value,
                Amount = entry.Score
            };
        });
        
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task FetchLeaderboard(string leaderboardId) {
        var lb = _settings.Leaderboards.FirstOrDefault(x => x.Id == leaderboardId);
        
        if (lb is null) {
            throw new Exception($"Leaderboard {leaderboardId} not found");
        }
        
        var db = _redis.GetDatabase();
        var lbKey = $"lb:{leaderboardId}";
        
        var query = from profileMember in _context.ProfileMembers
            join farmingWeight in _context.FarmingWeights on profileMember.Id equals farmingWeight.ProfileMemberId
            join profile in _context.Profiles on profileMember.ProfileId equals profile.ProfileId
            join minecraftAccount in _context.MinecraftAccounts on profileMember.PlayerUuid equals minecraftAccount.Id
            where farmingWeight.TotalWeight > 0
            orderby farmingWeight.TotalWeight descending
            select new LeaderboardEntry<double> {
                MemberId = profileMember.Id.ToString(),
                Profile = profileMember.Profile.ProfileName,
                Ign = profileMember.MinecraftAccount.Name,
                Amount = farmingWeight.TotalWeight
            };

        var scores = await query.Take(lb.Limit).ToListAsync();

        if (scores.Count == 0) return;

        var expiry = DateTime.UtcNow.AddSeconds(_settings.CompleteRefreshInterval);

        foreach (var score in scores) {
            db.HashSet(score.MemberId, new[] {
                new HashEntry("profile", score.Profile),
                new HashEntry("ign", score.Ign),
            }, CommandFlags.FireAndForget);
        }
        
        var entries = scores.Select(x => 
            new SortedSetEntry(x.MemberId, x.Amount)).ToArray();
        
        var transaction = db.CreateTransaction();
        
        // Intentionally not awaiting for use in the transaction
        #pragma warning disable CS4014 
        transaction.KeyDeleteAsync(lbKey);
        transaction.SortedSetAddAsync(lbKey, entries);
        transaction.KeyExpireAsync(lbKey, expiry);
        #pragma warning restore CS4014        
        
        await transaction.ExecuteAsync();
    }
}

public class LeaderboardEntry<T> {
    public required string MemberId { get; set; }
    public string? Ign { get; set; }
    public string? Profile { get; set; }
    public required T Amount { get; set; }
}