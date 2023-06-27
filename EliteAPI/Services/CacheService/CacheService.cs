using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Services.MojangService;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services.CacheService;

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly DataContext _context;
    private readonly IMojangService _mojangService;
    private readonly ConfigCooldownSettings _coolDowns;

    public CacheService(DataContext dataContext, IConnectionMultiplexer redis, IMojangService mojangService, IOptions<ConfigCooldownSettings> coolDowns)
    {
        _context = dataContext;
        _redis = redis;
        _mojangService = mojangService;
        _coolDowns = coolDowns.Value;
    }

    public async Task<string?> GetUsernameFromUuid(string uuid)
    {
        var found = await _redis.GetDatabase().StringGetAsync($"username:{uuid}");
        if (found.HasValue)
        {
            return found.ToString();
        }

        var account = await _mojangService.GetMinecraftAccountByUuid(uuid);
        if (account is null) return null;

        await _redis.GetDatabase().StringSetAsync($"username:{uuid}", account.Name, TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown));
        return account.Name;
    }

    public async Task<string?> GetUuidFromUsername(string username)
    {
        var found = await _redis.GetDatabase().StringGetAsync($"uuid:{username}");
        if (found.HasValue)
        {
            return found.ToString();
        }

        var account = await _mojangService.GetMinecraftAccountByIgn(username);
        if (account is null) return null;

        await _redis.GetDatabase().StringSetAsync($"uuid:{username}", account.Id, TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown));
        return account.Id;
    }

    public void SetUsernameUuidCombo(string username, string uuid)
    {
        var db = _redis.GetDatabase();

        db.StringSet($"username:{uuid}", username, TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown));
        db.StringSet($"uuid:{username}", uuid, TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown));
    }
}
