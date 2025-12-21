using EliteAPI.Configuration.Settings;
using EliteAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services;

public class CacheService(IConnectionMultiplexer redis, IOptions<ConfigCooldownSettings> coolDowns)
	: ICacheService {
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

	public async Task<string?> GetUsernameFromUuid(string uuid) {
		var db = redis.GetDatabase();
		if (await db.KeyExistsAsync($"username:{uuid}")) return await db.StringGetAsync($"username:{uuid}");

		return null;
	}

	public async Task<string?> GetUuidFromUsername(string username) {
		var db = redis.GetDatabase();
		if (await db.KeyExistsAsync($"uuid:{username}")) return await db.StringGetAsync($"uuid:{username}");

		return null;
	}

	public void SetUsernameUuidCombo(string username, string uuid, TimeSpan? expiry = null) {
		var db = redis.GetDatabase();

		expiry ??= TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown);

		db.StringSet($"username:{uuid}", username, (TimeSpan) expiry);
		db.StringSet($"uuid:{username}", uuid, (TimeSpan) expiry);
	}

	public async Task<bool> IsContestUpdateRequired(long contestId) {
		var db = redis.GetDatabase();
		var found = await db.StringGetAsync($"c:{contestId}");

		return found.IsNullOrEmpty || found.ToString() == "0";
	}

	public void SetContest(long contestId, bool claimed = false) {
		redis.GetDatabase().StringSet($"c:{contestId}", claimed ? "1" : "0", TimeSpan.FromHours(1));
	}
}