using EliteAPI.Config.Settings;
using EliteAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Services.LeaderboardService; 

public class LeaderboardService : ILeaderboardService {
    
    private readonly DataContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ConfigLeaderboardSettings _settings;

    public LeaderboardService(DataContext dataContext, IConnectionMultiplexer redis, IOptions<ConfigLeaderboardSettings> lbSettings) {
        _context = dataContext;
        _redis = redis;
        _settings = lbSettings.Value;
    }
    
    public async Task<List<LeaderboardEntry>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20) {
        var db = _redis.GetDatabase();
        
        var exists = await db.KeyExistsAsync($"lb:{leaderboardId}");
        if (!exists) await FetchLeaderboard(leaderboardId);

        return await GetSlice(leaderboardId, offset, limit);
    }

    public async Task<List<LeaderboardEntry>> GetSkillLeaderboardSlice(string skillName, int offset = 0, int limit = 20) {
        var db = _redis.GetDatabase();
        
        var exists = await db.KeyExistsAsync($"lb:{skillName}");
        if (!exists) await FetchSkillLeaderboard(skillName);

        return await GetSlice(skillName, offset, limit);
    }

    public async Task<List<LeaderboardEntry>> GetCollectionLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20) {
        var db = _redis.GetDatabase();
        
        var exists = await db.KeyExistsAsync($"lb:{leaderboardId}");
        if (!exists) await FetchCollectionLeaderboard(leaderboardId);

        return await GetSlice(leaderboardId, offset, limit);
    }

    private async Task<List<LeaderboardEntry>> GetSlice(string leaderboardId, int offset = 0, int limit = 20) {
        var db = _redis.GetDatabase();

        var slice = await db.SortedSetRangeByScoreWithScoresAsync(
            $"lb:{leaderboardId}", 
            order: Order.Descending, 
            skip: offset, 
            take: limit
        );
        
        if (slice.Length == 0) {
            return new List<LeaderboardEntry>();
        }
        
        // Get the hashset for each member
        var tasks = slice.Select(async entry => {
            var memberId = entry.Element.ToString();
            var member = await db.HashGetAllAsync(memberId);
            return new LeaderboardEntry {
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
            select new LeaderboardEntry {
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

    private async Task FetchSkillLeaderboard(string leaderboardId) {
        if (!_settings.SkillLeaderboards.TryGetValue(leaderboardId, out var lbSettings)) {
            throw new Exception($"Skill leaderboard {leaderboardId} not found");
        }
        
        // Ensure leaderboard Id corresponds to a skill column with reflection
        var skillProperty = typeof(Skills).GetProperty(lbSettings.Id);
        if (skillProperty is null) {
            throw new Exception($"Skill leaderboard {leaderboardId} not found");
        }

        var scores = await _context.Skills
            .Include(s => s.ProfileMember)
            .ThenInclude(pm => pm!.Profile)
            .Include(p => p.ProfileMember)
            .ThenInclude(pm => pm!.MinecraftAccount)
            .Where(s => EF.Property<double>(s, lbSettings.Id) > 0 && s.ProfileMember != null)
            .OrderByDescending(s => EF.Property<double>(s, lbSettings.Id))
            .Take(1000)
            .Select(s => new LeaderboardEntry {
                MemberId = s.ProfileMemberId.ToString(),
                Amount = EF.Property<double>(s, lbSettings.Id),
                Profile = s.ProfileMember!.Profile.ProfileName,
                Ign = s.ProfileMember.MinecraftAccount.Name
            })
            .ToListAsync();

        if (scores.Count == 0) return;
        
        await StoreLeaderboardEntries(scores, leaderboardId);
    }
    
    private async Task FetchCollectionLeaderboard(string leaderboardId) {
        if (!_settings.CollectionLeaderboards.TryGetValue(leaderboardId, out var lbSettings)) {
            throw new Exception($"Collection leaderboard {leaderboardId} not found");
        }
        
        var scores = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.MinecraftAccount)
            .Where(p => 
                EF.Functions.JsonExists(p.Collections, lbSettings.Id) 
                && p.Collections.RootElement.GetProperty(lbSettings.Id).GetInt64() > 0)
            .OrderByDescending(p => p.Collections.RootElement.GetProperty(lbSettings.Id).GetInt64())
            .Take(1000)
            .Select(p => new LeaderboardEntry {
                MemberId = p.Id.ToString(),
                Amount = p.Collections.RootElement.GetProperty(lbSettings.Id).GetInt64(),
                Profile = p.Profile.ProfileName,
                Ign = p.MinecraftAccount.Name
            })
            .ToListAsync();

        if (scores.Count == 0) return;
        
        await StoreLeaderboardEntries(scores, leaderboardId);
    }
    
    private async Task StoreLeaderboardEntries(List<LeaderboardEntry> entries, string leaderboardId) {
        var db = _redis.GetDatabase();
        var lbKey = $"lb:{leaderboardId}";

        var expiry = DateTime.UtcNow.AddSeconds(_settings.CompleteRefreshInterval);

        foreach (var score in entries) {
            db.HashSet(score.MemberId, new[] {
                new HashEntry("profile", score.Profile),
                new HashEntry("ign", score.Ign),
            }, CommandFlags.FireAndForget);
        }
        
        var sortedSetEntries = entries.Select(x => 
            new SortedSetEntry(x.MemberId, x.Amount)).ToArray();
        
        var transaction = db.CreateTransaction();
        
        // Intentionally not awaiting for use in the transaction
        #pragma warning disable CS4014 
        transaction.KeyDeleteAsync(lbKey);
        transaction.SortedSetAddAsync(lbKey, sortedSetEntries);
        transaction.KeyExpireAsync(lbKey, expiry);
        #pragma warning restore CS4014        
        
        await transaction.ExecuteAsync();
    }

}
public class LeaderboardEntry {
    public required string MemberId { get; init; }
    public string? Ign { get; init; }
    public string? Profile { get; init; }
    public double Amount { get; init; }
}