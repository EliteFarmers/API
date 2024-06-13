using EliteAPI.Config.Settings;
using EliteAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services;

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ConfigCooldownSettings _coolDowns;

    public CacheService(IConnectionMultiplexer redis, IOptions<ConfigCooldownSettings> coolDowns)
    {
        _redis = redis;
        _coolDowns = coolDowns.Value;
    }

    public async Task<string?> GetUsernameFromUuid(string uuid)
    {
        var db = _redis.GetDatabase();
        if (await db.KeyExistsAsync($"username:{uuid}")) {
            return await db.StringGetAsync($"username:{uuid}");
        }

        return null;
    }

    public async Task<string?> GetUuidFromUsername(string username)
    {
        var db = _redis.GetDatabase();
        if (await db.KeyExistsAsync($"uuid:{username}")) {
            return await db.StringGetAsync($"uuid:{username}");
        }

        return null;
    }

    public void SetUsernameUuidCombo(string username, string uuid, TimeSpan? expiry = null)
    {
        var db = _redis.GetDatabase();
        
        expiry ??= TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown);

        db.StringSet($"username:{uuid}", username, expiry);
        db.StringSet($"uuid:{username}", uuid, expiry);
    }

    public async Task<bool> IsContestUpdateRequired(long contestId) {
        var db = _redis.GetDatabase();
        var found = await db.StringGetAsync($"c:{contestId}");

        return found.IsNullOrEmpty || found.ToString() == "0";
    }

    public void SetContest(long contestId, bool claimed = false) {
        _redis.GetDatabase().StringSet($"c:{contestId}", claimed ? "1" : "0");
    }
}
