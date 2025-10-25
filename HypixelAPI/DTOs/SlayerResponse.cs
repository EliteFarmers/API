using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class RawSlayerData
{
    [JsonPropertyName("slayer_quest")] public SlayerQuestResponse? SlayerQuest { get; set; }
    [JsonPropertyName("slayer_bosses")] public MemberSlayerBossesResponse? SlayerBosses { get; set; }
}

public class SlayerQuestResponse
{
    public string? Type { get; set; }
    public int? Tier { get; set; }

    [JsonPropertyName("start_timestamp")] public long? StartTimestamp { get; set; }
    [JsonPropertyName("completion_state")] public int? CompletionState { get; set; }
    [JsonPropertyName("used_armor")] public bool? UsedArmor { get; set; }
    public bool? Solo { get; set; }
    [JsonPropertyName("combat_xp")] public double? CombatXp { get; set; }
    [JsonPropertyName("last_killed_mob_island")] public string? LastKilledMobIsland { get; set; }

    [JsonPropertyName("xp_on_last_follower_spawn")] public int? XpOnLastFollowerSpawn { get; set; }
    [JsonPropertyName("spawn_timestamp")] public long? SpawnTimestamp { get; set; }

    [JsonPropertyName("recent_mob_kills")] public List<SlayerRecentMobKill>? RecentMobKills { get; set; }
}

public class SlayerRecentMobKill
{
    public double? Xp { get; set; }
    public long? Timestamp { get; set; }
}

public class MemberSlayerBossesResponse
{
    [JsonPropertyName("zombie")] public SlayerBossProgress? Zombie { get; set; }
    [JsonPropertyName("spider")] public SlayerBossProgress? Spider { get; set; }
    [JsonPropertyName("wolf")] public SlayerBossProgress? Wolf { get; set; }
    [JsonPropertyName("enderman")] public SlayerBossProgress? Enderman { get; set; }
    [JsonPropertyName("blaze")] public SlayerBossProgress? Blaze { get; set; }
    [JsonPropertyName("vampire")] public SlayerBossProgress? Vampire { get; set; }
}

public class SlayerBossProgress
{
    [JsonPropertyName("claimed_levels")] public Dictionary<string, bool> ClaimedLevels { get; set; } = new();

    // Total XP for the boss
    public long? Xp { get; set; }

    // Kills per tier (0..5). Nullable to be tolerant of missing tiers in samples.
    [JsonPropertyName("boss_kills_tier_0")] public int? BossKillsTier0 { get; set; }
    [JsonPropertyName("boss_kills_tier_1")] public int? BossKillsTier1 { get; set; }
    [JsonPropertyName("boss_kills_tier_2")] public int? BossKillsTier2 { get; set; }
    [JsonPropertyName("boss_kills_tier_3")] public int? BossKillsTier3 { get; set; }
    [JsonPropertyName("boss_kills_tier_4")] public int? BossKillsTier4 { get; set; }
    [JsonPropertyName("boss_kills_tier_5")] public int? BossKillsTier5 { get; set; }

    // Attempts per tier (0..5). Nullable.
    [JsonPropertyName("boss_attempts_tier_0")] public int? BossAttemptsTier0 { get; set; }
    [JsonPropertyName("boss_attempts_tier_1")] public int? BossAttemptsTier1 { get; set; }
    [JsonPropertyName("boss_attempts_tier_2")] public int? BossAttemptsTier2 { get; set; }
    [JsonPropertyName("boss_attempts_tier_3")] public int? BossAttemptsTier3 { get; set; }
    [JsonPropertyName("boss_attempts_tier_4")] public int? BossAttemptsTier4 { get; set; }
    [JsonPropertyName("boss_attempts_tier_5")] public int? BossAttemptsTier5 { get; set; }
}
