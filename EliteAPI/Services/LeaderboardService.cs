using System.Diagnostics.CodeAnalysis;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services; 

public class LeaderboardService(
    DataContext dataContext, 
    IConnectionMultiplexer redis,
    IOptions<ConfigLeaderboardSettings> lbSettings,
    IBackgroundTaskQueue taskQueue
) : ILeaderboardService 
{
    private readonly ConfigLeaderboardSettings _settings = lbSettings.Value;

    private static readonly RedisValue ProfileHash = "profile";
    private static readonly RedisValue IgnHash = "ign";
    private static readonly RedisValue UuidHash = "uuid";

    public async Task<List<LeaderboardEntry>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20) {
        if (!TryGetLeaderboardSettings(leaderboardId, out var settings)) return [];
        await UpdateLeaderboardIfNeeded(leaderboardId);

        return await GetSlice(leaderboardId, offset, Math.Min(limit, settings?.Limit ?? limit));
    }

    private async Task UpdateLeaderboardIfNeeded(string leaderboardId) {
        var db = redis.GetDatabase();
        var key = $"lb:updated:{leaderboardId}";
        
        // If key exists, lb does not need updating
        // This also prevents repeated calls from triggering it twice
        if (await db.KeyExistsAsync(key)) return;
        
        var expiry = DateTime.UtcNow.AddSeconds(_settings.CompleteRefreshInterval);
        // Set updated
        await db.StringSetAsync(key, value: "1");
        await db.StringGetSetExpiryAsync(key, expiry);
        
        // Enqueue refresh task
        await taskQueue.EnqueueAsync(async (scope, ct) => {
            var lbService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
            
            await lbService.FetchLeaderboard(leaderboardId);
        });
    }

    public async Task<List<LeaderboardEntryWithRank>> GetLeaderboardSliceAtScore(string leaderboardId, double score, int limit = 5, string? excludeMemberId = null) {
        if (!TryGetLeaderboardSettings(leaderboardId, out var lb)) return [];
        
        // var memberRank = excludeMemberId is null ? -1 : await GetLeaderboardPositionNoCheck(leaderboardId, excludeMemberId);
        
        var db = redis.GetDatabase();
        var exists = await db.KeyExistsAsync($"lb:{leaderboardId}");
        if (!exists) await GetLeaderboardSlice(leaderboardId); // Will populate the leaderboard if it doesn't exist
        
        var slice = await db.SortedSetRangeByScoreWithScoresAsync(
            $"lb:{leaderboardId}", 
            order: Order.Descending, 
            start: score,
            skip: 0, 
            take: limit
        );
        
        if (slice.Length == 0) {
            return [];
        }
        
        var firstRank = await db.SortedSetRankAsync($"lb:{leaderboardId}", slice.First().Element, Order.Descending) ?? 1;
        var tasks = GetLeaderboardEntriesWithScore(slice, leaderboardId, (int) firstRank, lb.Profile);
        
        return (await Task.WhenAll(tasks)).ToList();
    }
    
    private IEnumerable<Task<LeaderboardEntry>> GetLeaderboardEntries(IEnumerable<SortedSetEntry> slice, string leaderboardId, bool profileLeaderboard = false) {
        var db = redis.GetDatabase();
        return slice.Select(async entry => {
            var memberId = entry.Element.ToString();

            if (profileLeaderboard) {
                var members = await db.StringGetAsync($"lb:{leaderboardId}:{memberId}:members");
                if (members.IsNullOrEmpty) {
                    return new LeaderboardEntry {
                        Uuid = memberId,
                        MemberId = memberId,
                        Amount = entry.Score
                    };
                }

                var values = members.ToString().Split("|");
                List<ProfileLeaderboardMember> entryMembers = [];
                for (var memberIndex = 1; memberIndex < values.Length; memberIndex += 3) {
                    entryMembers.Add(new ProfileLeaderboardMember {
                        Uuid = values[memberIndex],
                        Ign = values[memberIndex + 1],
                        Xp = int.Parse(values[memberIndex + 2])
                    });
                }
            
                return new LeaderboardEntry {
                    Uuid = memberId,
                    MemberId = memberId,
                    Profile = values[0],
                    Amount = entry.Score,
                    Members = entryMembers
                };
            }
            
            var member = await db.HashGetAllAsync(memberId);
            return new LeaderboardEntry {
                MemberId = memberId,
                Profile = member.FirstOrDefault(x => x.Name == ProfileHash).Value,
                Ign = member.FirstOrDefault(x => x.Name == IgnHash).Value,
                Uuid = member.FirstOrDefault(x => x.Name == UuidHash).Value,
                Amount = entry.Score
            };
        });
    }
    private IEnumerable<Task<LeaderboardEntryWithRank>> GetLeaderboardEntriesWithScore(IEnumerable<SortedSetEntry> slice, string leaderboardId, int firstRank, bool profileLeaderboard = false) {
        var db = redis.GetDatabase();
        return slice.Select(async (entry, i) => {
            var memberId = entry.Element.ToString();
            
            if (profileLeaderboard) {
                var members = await db.StringGetAsync($"lb:{leaderboardId}:{memberId}:members");
                if (members.IsNullOrEmpty) {
                    return new LeaderboardEntryWithRank {
                        MemberId = memberId,
                        Amount = entry.Score,
                        Rank = i + firstRank
                    };
                }

                var values = members.ToString().Split("|");
                List<ProfileLeaderboardMember> entryMembers = [];
                for (var memberIndex = 1; memberIndex < values.Length; memberIndex += 3) {
                    entryMembers.Add(new ProfileLeaderboardMember {
                        Uuid = values[memberIndex],
                        Ign = values[memberIndex + 1],
                        Xp = int.Parse(values[memberIndex + 2])
                    });
                }
            
                return new LeaderboardEntryWithRank {
                    MemberId = memberId,
                    Amount = entry.Score,
                    Members = entryMembers,
                    Profile = values[0],
                    Rank = i + firstRank
                };
            }
            
            var member = await db.HashGetAllAsync(memberId);
            return new LeaderboardEntryWithRank {
                MemberId = memberId,
                Profile = member.FirstOrDefault(x => x.Name == ProfileHash).Value,
                Ign = member.FirstOrDefault(x => x.Name == IgnHash).Value,
                Uuid = member.FirstOrDefault(x => x.Name == UuidHash).Value,
                Amount = entry.Score,
                Rank = i + firstRank
            };
        });
    }

    public async Task<LeaderboardPositionsDto> GetLeaderboardPositions(string memberId, string? profileId = null) {
        var result = new LeaderboardPositionsDto();
        
        // Can't use parallel foreach because of database connection
        foreach (var lbId in _settings.Leaderboards.Keys) {
            result.Misc.Add(lbId, await GetLeaderboardPositionNoCheck(lbId, memberId));
        }

        foreach (var skillName in _settings.SkillLeaderboards.Keys) {
            result.Skills.Add(skillName, await GetLeaderboardPositionNoCheck(skillName, memberId));
        }
        
        foreach (var lbId in _settings.CollectionLeaderboards.Keys) {
            result.Collections.Add(lbId, await GetLeaderboardPositionNoCheck(lbId, memberId));
        }
        
        foreach (var lbId in _settings.PestLeaderboards.Keys) {
            result.Pests.Add(lbId, await GetLeaderboardPositionNoCheck(lbId, memberId));
        }
        
        if (profileId is not null) {
            foreach (var lbId in _settings.ProfileLeaderboards.Keys) {
                result.Profile.Add(lbId, await GetLeaderboardPositionNoCheck(lbId, profileId));
            }
        }
        
        return result;
    }

    private async Task<int> GetLeaderboardPositionNoCheck(string leaderboardId, string memberId) {
        await UpdateLeaderboardIfNeeded(leaderboardId);
        
        var db = redis.GetDatabase();
        var rank = await db.SortedSetRankAsync($"lb:{leaderboardId}", memberId, Order.Descending);
        
        return rank.HasValue ? (int) rank.Value + 1 : -1;
    }

    public async Task<(int rank, double score)> GetLeaderboardPositionAndScore(string leaderboardId, string memberId, bool includeScore = true) {
        if (!LeaderboardExists(leaderboardId)) return (-1, 0);
        
        var rank = await GetLeaderboardPositionNoCheck(leaderboardId, memberId);
        
        if (!includeScore) return (rank, 0);

        var db = redis.GetDatabase();
        var score = await db.SortedSetScoreAsync($"lb:{leaderboardId}", memberId) ?? 0;
        
        return (rank, score);
    }
    
    private async Task<List<LeaderboardEntry>> GetSlice(string leaderboardId, int offset = 0, int limit = 20) {
        if (!TryGetLeaderboardSettings(leaderboardId, out var lb)) return [];
        var db = redis.GetDatabase();

        var slice = await db.SortedSetRangeByScoreWithScoresAsync(
            $"lb:{leaderboardId}", 
            order: Order.Descending, 
            skip: offset, 
            take: limit
        );
        
        if (slice.Length == 0) {
            return [];
        }
        
        // Get the hashset for each member
        var tasks = GetLeaderboardEntries(slice, leaderboardId, lb.Profile);
        var entries = (await Task.WhenAll(tasks)).ToList();
        return entries;
    }
    
    public async Task RemoveMemberFromAllLeaderboards(string memberId) {
        var lbs = _settings.Leaderboards.Keys
            .Concat(_settings.CollectionLeaderboards.Keys)
            .Concat(_settings.SkillLeaderboards.Keys)
            .Concat(_settings.PestLeaderboards.Keys);
        
        await RemoveMemberFromLeaderboards(lbs, memberId);
    }

    public async Task RemoveMemberFromLeaderboards(IEnumerable<string> leaderboardIds, string memberId) {
        var db = redis.GetDatabase();

        await Task.WhenAll(leaderboardIds.Select(async lbId => {
            await db.SortedSetRemoveAsync($"lb:{lbId}", memberId);
        }));
    }

    public async Task FetchLeaderboard(string leaderboardId) {
        if (_settings.Leaderboards.ContainsKey(leaderboardId)) {
            await FetchMiscLeaderboard(leaderboardId);
            return;
        }
        
        if (_settings.SkillLeaderboards.ContainsKey(leaderboardId)) {
            await FetchSkillLeaderboard(leaderboardId);
            return;
        }
        
        if (_settings.CollectionLeaderboards.ContainsKey(leaderboardId)) {
            await FetchCollectionLeaderboard(leaderboardId);
            return;
        }
        
        if (_settings.PestLeaderboards.ContainsKey(leaderboardId)) {
            await FetchPestLeaderboard(leaderboardId);
            return;
        }
        
        if (_settings.ProfileLeaderboards.ContainsKey(leaderboardId)) {
            await FetchProfileLeaderboard(leaderboardId);
        }
    }

    private async Task FetchMiscLeaderboard(string leaderboardId) {
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

        var scores = await dataContext.Skills
            .AsNoTracking()
            .Include(p => p.ProfileMember)
            .ThenInclude(pm => pm!.MinecraftAccount)
            .Where(s => EF.Property<double>(s, lbSettings.Id) > 0 && s.ProfileMember != null && !s.ProfileMember.WasRemoved)
            .OrderByDescending(s => EF.Property<double>(s, lbSettings.Id))
            .Take(lbSettings.Limit)
            .Select(s => new LeaderboardEntry {
                MemberId = s.ProfileMemberId.ToString(),
                Amount = EF.Property<double>(s, lbSettings.Id),
                Profile = s.ProfileMember!.ProfileName ?? s.ProfileMember.Profile.ProfileName,
                Uuid = s.ProfileMember!.PlayerUuid,
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
        
        var scores = await dataContext.ProfileMembers
            .AsNoTracking()
            .Include(p => p.MinecraftAccount)
            .Where(p => 
                !p.WasRemoved
                && EF.Functions.JsonExists(p.Collections, lbSettings.Id) 
                && p.Collections.RootElement.GetProperty(lbSettings.Id).GetInt64() > 0)
            .OrderByDescending(p => p.Collections.RootElement.GetProperty(lbSettings.Id).GetInt64())
            .Take(lbSettings.Limit)
            .Select(p => new LeaderboardEntry {
                MemberId = p.Id.ToString(),
                Amount = p.Collections.RootElement.GetProperty(lbSettings.Id).GetInt64(),
                Profile = p.ProfileName ?? p.Profile.ProfileName,
                Uuid = p.PlayerUuid,
                Ign = p.MinecraftAccount.Name
            })
            .ToListAsync();

        if (scores.Count == 0) return;
        
        await StoreLeaderboardEntries(scores, leaderboardId);
    }

    private async Task FetchPestLeaderboard(string leaderboardId) {
        if (!_settings.PestLeaderboards.TryGetValue(leaderboardId, out var lbSettings)) {
            throw new Exception($"Pest leaderboard {leaderboardId} not found");
        }

        // Ensure leaderboard Id corresponds to a pest column with reflection
        var pestProperty = typeof(Pests).GetProperty(lbSettings.Id);
        if (pestProperty is null) {
            throw new Exception($"Pest leaderboard {leaderboardId} not found");
        }

        var scores = await dataContext.ProfileMembers
            .AsNoTracking()
            .Include(p => p.MinecraftAccount)
            .Include(p => p.Farming)
            .Where(p => !p.WasRemoved && EF.Property<int>(p.Farming.Pests, lbSettings.Id) > 0)
            .OrderByDescending(p => EF.Property<int>(p.Farming.Pests, lbSettings.Id))
            .Take(lbSettings.Limit)
            .Select(p => new LeaderboardEntry {
                MemberId = p.Id.ToString(),
                Amount = EF.Property<int>(p.Farming.Pests, lbSettings.Id),
                Profile = p.ProfileName ?? p.Profile.ProfileName,
                Uuid = p.PlayerUuid,
                Ign = p.MinecraftAccount.Name
            })
            .ToListAsync();

        if (scores.Count == 0) return;
        
        await StoreLeaderboardEntries(scores, leaderboardId);
    }
    
    private async Task StoreLeaderboardEntries(List<LeaderboardEntry> entries, string leaderboardId) {
        var db = redis.GetDatabase();
        var lbKey = $"lb:{leaderboardId}";

        foreach (var score in entries) {
            db.HashSet(score.MemberId, new[] {
                new HashEntry(ProfileHash, score.Profile),
                new HashEntry(IgnHash, score.Ign),
                new HashEntry(UuidHash, score.Uuid ?? string.Empty)
            }, CommandFlags.FireAndForget);
        }
        
        var sortedSetEntries = entries.Select(x => 
            new SortedSetEntry(x.MemberId, x.Amount)).ToArray();
        
        var transaction = db.CreateTransaction();
        
        // Intentionally not awaiting use in the transaction
        #pragma warning disable CS4014 
        transaction.KeyDeleteAsync(lbKey);
        transaction.SortedSetAddAsync(lbKey, sortedSetEntries);
        #pragma warning restore CS4014        
        
        await transaction.ExecuteAsync();
    }

    private async Task StoreProfileLeaderboardEntries(List<LeaderboardEntry> entries, string leaderboardId) {
        var db = redis.GetDatabase();
        var lbKey = $"lb:{leaderboardId}";
        var expiry = TimeSpan.FromSeconds(_settings.CompleteRefreshInterval * 2);
        
        foreach (var score in entries) {
            if (score.Members is null || score.Members.Count < 1) continue;
            
            var stringified = string.Join('|', score.Members.Select(m => $"{m.Uuid}|{m.Ign}|{m.Xp}"));
            db.StringSet($"{lbKey}:{score.MemberId}:members", score.Profile + "|" + stringified, expiry, When.Always, CommandFlags.FireAndForget);
        }
        
        var sortedSetEntries = entries
            .Select(x => new SortedSetEntry(x.MemberId, x.Amount)).ToArray();
        
        var transaction = db.CreateTransaction();
        
        // Intentionally not awaiting use in the transaction
        #pragma warning disable CS4014 
        transaction.KeyDeleteAsync(lbKey);
        transaction.SortedSetAddAsync(lbKey, sortedSetEntries);
        #pragma warning restore CS4014        
        
        await transaction.ExecuteAsync();
    }
    
    private IQueryable<LeaderboardEntry> GetSpecialLeaderboardQuery(string leaderboardId) {
        if (!_settings.Leaderboards.TryGetValue(leaderboardId, out var lb)) {
            throw new Exception($"Leaderboard {leaderboardId} not found");
        }

        var query = dataContext.ProfileMembers
            .AsNoTracking()
            .Include(p => p.MinecraftAccount);
        
        switch (leaderboardId)
        {
            case "farmingweight":
                return (from member in query
                    join farmingWeight in dataContext.Farming on member.Id equals farmingWeight.ProfileMemberId
                    where farmingWeight.TotalWeight > 0 && !member.WasRemoved
                    orderby farmingWeight.TotalWeight descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.ProfileName ?? member.Profile.ProfileName,
                        Amount = farmingWeight.TotalWeight,
                        MemberId = member.Id.ToString(),
                        Uuid = member.PlayerUuid
                    }).Take(lb.Limit);
            
            case "chocolate":
                return (from member in query
                    join chocolateFactory in dataContext.ChocolateFactories on member.Id equals chocolateFactory.ProfileMemberId
                    where chocolateFactory.TotalChocolate > 0 && !member.WasRemoved
                    orderby chocolateFactory.TotalChocolate descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.ProfileName ?? member.Profile.ProfileName,
                        Amount = chocolateFactory.TotalChocolate,
                        MemberId = member.Id.ToString(),
                        Uuid = member.PlayerUuid
                    }).Take(lb.Limit);

            case "diamondmedals" or "platinummedals" or "goldmedals" or "silvermedals" or "bronzemedals": 
                var medal = leaderboardId.Replace("medals", "");
                // Capitalize first letter
                medal = medal.First().ToString().ToUpper() + medal[1..];

                return (from member in query
                    join jacobData in dataContext.JacobData on member.Id equals jacobData.ProfileMemberId
                    where EF.Property<int>(jacobData.EarnedMedals, medal) > 0 && !member.WasRemoved
                    orderby EF.Property<int>(jacobData.EarnedMedals, medal) descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.ProfileName ?? member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Uuid = member.PlayerUuid,
                        Amount = EF.Property<int>(jacobData.EarnedMedals, medal)
                    }).Take(lb.Limit);
            
            case "participations":
                return (from member in query
                    join jacobData in dataContext.JacobData on member.Id equals jacobData.ProfileMemberId
                    where jacobData.Participations > 0 && !member.WasRemoved
                    orderby jacobData.Participations descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.ProfileName ?? member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Uuid = member.PlayerUuid,
                        Amount = jacobData.Participations
                    }).Take(lb.Limit);
            
            case "skyblockxp":
                return (from member in query
                    where member.SkyblockXp > 0 && !member.WasRemoved
                    orderby member.SkyblockXp descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.ProfileName ?? member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Uuid = member.PlayerUuid,
                        Amount = member.SkyblockXp
                    }).Take(lb.Limit);
            
            case "firstplace":
                return (from member in query
                    join jacobData in dataContext.JacobData on member.Id equals jacobData.ProfileMemberId
                    where jacobData.FirstPlaceScores > 0 && !member.WasRemoved
                    orderby jacobData.FirstPlaceScores descending
                    select new LeaderboardEntry {
                        Ign = member.MinecraftAccount.Name,
                        Profile = member.ProfileName ?? member.Profile.ProfileName,
                        MemberId = member.Id.ToString(),
                        Uuid = member.PlayerUuid,
                        Amount = jacobData.FirstPlaceScores
                    }).Take(lb.Limit);

            default:
                throw new Exception($"Leaderboard {leaderboardId} not found");
        }
    }
    
    private async Task FetchProfileLeaderboard(string leaderboardId) {
        if (!_settings.ProfileLeaderboards.TryGetValue(leaderboardId, out var lbSettings)) {
            throw new Exception($"Profile leaderboard {leaderboardId} not found");
        }

        var query = dataContext.Gardens.AsNoTracking().AsSplitQuery()
            .Include(g => g.Profile)
            .ThenInclude(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .Where(g => g.GardenExperience > 0 && g.Profile.Members.Any(m => !m.WasRemoved));
        
        List<LeaderboardEntry> scores;
        
        switch (leaderboardId) {
            case "garden":
                scores = await query
                    .OrderByDescending(g => g.GardenExperience)
                    .Take(lbSettings.Limit)
                    .Select(g => new LeaderboardEntry {
                        Amount = g.GardenExperience,
                        MemberId = g.ProfileId,
                        Profile = g.Profile.Members.First(m => !m.WasRemoved).ProfileName,
                        Members = g.Profile!.Members
                            .Where(m => !m.WasRemoved)
                            .Select(m => new ProfileLeaderboardMember() {
                                Ign = m.MinecraftAccount.Name,
                                Uuid = m.PlayerUuid,
                                Xp = m.SkyblockXp
                            })
                            .OrderByDescending(m => m.Xp)
                            .ToList()
                    }).ToListAsync();
                
                break;
            case "visitors-accepted":
                scores = await query
                    .Where(g => g.CompletedVisitors > 0)
                    .OrderByDescending(g => g.CompletedVisitors)
                    .Take(lbSettings.Limit)
                    .Select(g => new LeaderboardEntry() {
                        Amount = g.CompletedVisitors,
                        MemberId = g.ProfileId,
                        Profile = g.Profile.Members.First(m => !m.WasRemoved).ProfileName,
                        Members = g.Profile!.Members
                            .Where(m => !m.WasRemoved)
                            .Select(m => new ProfileLeaderboardMember() {
                                Ign = m.MinecraftAccount.Name,
                                Uuid = m.PlayerUuid,
                                Xp = m.SkyblockXp
                            })
                            .OrderByDescending(m => m.Xp)
                            .ToList()
                    }).ToListAsync();
                break;
            default: // Crop milestone leaderboards
                scores = await query
                    .Where(g => EF.Property<long>(g.Crops, lbSettings.Id) > 0)
                    .OrderByDescending(g => EF.Property<long>(g.Crops, lbSettings.Id))
                    .Take(lbSettings.Limit)
                    .Select(g => new LeaderboardEntry() {
                        Amount = EF.Property<long>(g.Crops, lbSettings.Id),
                        MemberId = g.ProfileId,
                        Profile = g.Profile.Members.First(m => !m.WasRemoved).ProfileName,
                        Members = g.Profile!.Members
                            .Where(m => !m.WasRemoved)
                            .Select(m => new ProfileLeaderboardMember() {
                                Ign = m.MinecraftAccount.Name,
                                Uuid = m.PlayerUuid,
                                Xp = m.SkyblockXp
                            })
                            .OrderByDescending(m => m.Xp)
                            .ToList()
                    }).ToListAsync();
                break;
        }

        await StoreProfileLeaderboardEntries(scores, leaderboardId);
    }


    private bool LeaderboardExists(string leaderboardId, bool includeProfile = true) {
        return _settings.CollectionLeaderboards.ContainsKey(leaderboardId)
               || _settings.SkillLeaderboards.ContainsKey(leaderboardId)
               || _settings.Leaderboards.ContainsKey(leaderboardId)
               || _settings.PestLeaderboards.ContainsKey(leaderboardId)
               || (includeProfile && _settings.ProfileLeaderboards.ContainsKey(leaderboardId));
    }

    public bool TryGetLeaderboardSettings(string leaderboardId, [NotNullWhen(true)] out Leaderboard? lb, bool includeProfile = true) {
        return _settings.CollectionLeaderboards.TryGetValue(leaderboardId, out lb)
               || _settings.SkillLeaderboards.TryGetValue(leaderboardId, out lb) 
               || _settings.Leaderboards.TryGetValue(leaderboardId, out lb)
               || _settings.PestLeaderboards.TryGetValue(leaderboardId, out lb)
               || (includeProfile && _settings.ProfileLeaderboards.TryGetValue(leaderboardId, out lb));
    }
    
    public void UpdateLeaderboardScore(string leaderboardId, string memberId, double score) {
        if (!LeaderboardExists(leaderboardId)) return;
        
        var db = redis.GetDatabase();
        db.SortedSetAddAsync($"lb:{leaderboardId}", memberId, score, When.Exists, CommandFlags.FireAndForget);
    }
}

public class ProfileLeaderboardMember {
    public required string Ign { get; init; }
    public required string Uuid { get; init; }
    public int Xp { get; init; }
}

public class LeaderboardEntry {
    public required string MemberId { get; init; }
    public string? Ign { get; init; }
    public string? Profile { get; init; }
    public double Amount { get; init; }
    public string? Uuid { get; init; }
    public List<ProfileLeaderboardMember>? Members { get; init; }
}

public class LeaderboardEntryWithRank : LeaderboardEntry {
    public int Rank { get; init; }
}