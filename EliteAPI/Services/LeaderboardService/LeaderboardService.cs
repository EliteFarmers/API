using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using EliteAPI.Models.Entities.Hypixel;
using Z.EntityFramework.Plus;

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
        if (exists) return await GetSlice(leaderboardId, offset, limit);
        
        if (_settings.Leaderboards.ContainsKey(leaderboardId)) {
            await FetchLeaderboard(leaderboardId);
        } else if (_settings.CollectionLeaderboards.ContainsKey(leaderboardId)) {
            await FetchCollectionLeaderboard(leaderboardId);
        } else if (_settings.SkillLeaderboards.ContainsKey(leaderboardId)) {
            await FetchSkillLeaderboard(leaderboardId);
        } else {
            return new List<LeaderboardEntry>();
        }
        
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

    public async Task<LeaderboardPositionsDto> GetLeaderboardPositions(string memberId) {
        var result = new LeaderboardPositionsDto();
        
        var db = _redis.GetDatabase();

        // Can't use parallel foreach because of database connection
        foreach (var lbId in _settings.Leaderboards.Keys) {
            if (true || !await db.KeyExistsAsync($"lb:{lbId}")) await FetchLeaderboard(lbId);
            var rank = await db.SortedSetRankAsync($"lb:{lbId}", memberId, Order.Descending);
            result.misc.Add(lbId, rank.HasValue ? (int) rank.Value + 1 : -1);
        }

        foreach (var skillName in _settings.SkillLeaderboards.Keys) {
            if (!await db.KeyExistsAsync($"lb:{skillName}")) await FetchSkillLeaderboard(skillName);
            var rank = await db.SortedSetRankAsync($"lb:{skillName}", memberId, Order.Descending);
            result.skills.Add(skillName, rank.HasValue ? (int) rank.Value + 1 : -1);
        }
        
        foreach (var lbId in _settings.CollectionLeaderboards.Keys) {
            if (!await db.KeyExistsAsync($"lb:{lbId}")) await FetchCollectionLeaderboard(lbId);
            var rank = await db.SortedSetRankAsync($"lb:{lbId}", memberId, Order.Descending);
            result.collections.Add(lbId, rank.HasValue ? (int) rank.Value + 1 : -1);
        }
        
        return result;
    }

    public async Task<int> GetLeaderboardPosition(string leaderboardId, string memberId) {
        if (!LeaderboardExists(leaderboardId)) return -1;
        
        var db = _redis.GetDatabase();
        var rank = await db.SortedSetRankAsync($"lb:{leaderboardId}", memberId, Order.Descending);
        
        return rank.HasValue ? (int) rank.Value + 1 : -1;
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
        if (!_settings.Leaderboards.ContainsKey(leaderboardId)) {
            throw new Exception($"Leaderboard {leaderboardId} not found");
        }
        
        var query = GetSpecialLeaderboardQuery(leaderboardId);
        var scores = await query.ToListAsync();

        if (scores.Count == 0) return;

        await StoreLeaderboardEntries(scores, leaderboardId);
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
            .Take(lbSettings.Limit)
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

    private IQueryable<LeaderboardEntry> GetSpecialLeaderboardQuery(string leaderboardId) {
        if (!_settings.Leaderboards.TryGetValue(leaderboardId, out var lb)) {
            throw new Exception($"Leaderboard {leaderboardId} not found");
        }

        var query = _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.MinecraftAccount);
        
        switch (leaderboardId)
        {
            case "farmingweight":
                return (from member in query
                    join farmingWeight in _context.FarmingWeights on member.Id equals farmingWeight.ProfileMemberId
                    where farmingWeight.TotalWeight > 0
                    orderby farmingWeight.TotalWeight descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.Profile.ProfileName,
                        Amount = farmingWeight.TotalWeight,
                        MemberId = member.Id.ToString()
                    }).Take(lb.Limit);

            case "goldmedals" or "silvermedals" or "bronzemedals": 
                var medal = leaderboardId.Replace("medals", "");
                // Capitalize first letter
                medal = medal.First().ToString().ToUpper() + medal[1..];

                return (from member in query
                    join jacobData in _context.JacobData on member.Id equals jacobData.ProfileMemberId
                    where EF.Property<int>(jacobData.EarnedMedals, medal) > 0
                    orderby EF.Property<int>(jacobData.EarnedMedals, medal) descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Amount = EF.Property<int>(jacobData.EarnedMedals, medal)
                    }).Take(lb.Limit);
            
            case "participations":
                return (from member in query
                    join jacobData in _context.JacobData on member.Id equals jacobData.ProfileMemberId
                    where jacobData.Participations > 0
                    orderby jacobData.Participations descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Amount = jacobData.Participations
                    }).Take(lb.Limit);
            
            case "skyblockxp":
                return (from member in query
                    where member.SkyblockXp > 0
                    orderby member.SkyblockXp descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Amount = member.SkyblockXp
                    }).Take(lb.Limit);
            
            case "firstplace":
                return _context.ProfileMembers
                    .IncludeOptimized(p => p.Profile)
                    .IncludeOptimized(p => p.MinecraftAccount)
                    .IncludeOptimized(p => p.JacobData)
                    .IncludeOptimized(p => p.JacobData.Contests)
                    .Select(p => new LeaderboardEntry {
                        Ign = p.MinecraftAccount.Name,
                        Profile = p.Profile.ProfileName,
                        MemberId = p.Id.ToString(),
                        Amount = p.JacobData.Contests.Count(c => c.Position == 0)
                    }).OrderByDescending(p => p.Amount).Take(lb.Limit);

            default:
                throw new Exception($"Leaderboard {leaderboardId} not found");
        }
    }

    private bool LeaderboardExists(string leaderboardId) {
        return _settings.CollectionLeaderboards.ContainsKey(leaderboardId)
               || _settings.SkillLeaderboards.ContainsKey(leaderboardId)
               || _settings.Leaderboards.ContainsKey(leaderboardId);
    }

    public bool TryGetLeaderboardSettings(string leaderboardId, out Leaderboard? lb) {
        return _settings.CollectionLeaderboards.TryGetValue(leaderboardId, out lb)
               || _settings.SkillLeaderboards.TryGetValue(leaderboardId, out lb) 
               || _settings.Leaderboards.TryGetValue(leaderboardId, out lb);
    }
}

public class LeaderboardEntry {
    public required string MemberId { get; init; }
    public string? Ign { get; init; }
    public string? Profile { get; init; }
    public double Amount { get; init; }
}