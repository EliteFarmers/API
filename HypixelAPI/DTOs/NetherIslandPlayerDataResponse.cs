using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class NetherIslandPlayerDataResponse
{
    [JsonPropertyName("selected_faction")] public string? SelectedFaction { get; set; }
    [JsonPropertyName("barbarians_reputation")] public double? BarbariansReputation { get; set; }
    [JsonPropertyName("mages_reputation")] public double? MagesReputation { get; set; }
    [JsonPropertyName("last_minibosses_killed")] public List<string> LastMinibossesKilled { get; set; } = [];
    [JsonPropertyName("kuudra_completed_tiers")] public KuudraCompletedTiersResponse? KuudraCompletedTiers { get; set; }
    [JsonPropertyName("kuudra_party_finder")] public KuudraPartyFinderResponse? KuudraPartyFinder { get; set; }
    [JsonPropertyName("dojo")] public NetherIslandDojoResponse? Dojo { get; set; }
}

public class NetherIslandDojoResponse
{
    [JsonPropertyName("dojo_points_mob_kb")] public int? MobKnockbackPoints { get; set; }
    [JsonPropertyName("dojo_time_mob_kb")] public int? MobKnockbackTime { get; set; }

    [JsonPropertyName("dojo_points_wall_jump")] public int? WallJumpPoints { get; set; }
    [JsonPropertyName("dojo_time_wall_jump")] public int? WallJumpTime { get; set; }

    [JsonPropertyName("dojo_points_archer")] public int? ArcherPoints { get; set; }
    [JsonPropertyName("dojo_time_archer")] public int? ArcherTime { get; set; }

    [JsonPropertyName("dojo_points_sword_swap")] public int? SwordSwapPoints { get; set; }
    [JsonPropertyName("dojo_time_sword_swap")] public int? SwordSwapTime { get; set; }

    [JsonPropertyName("dojo_points_snake")] public int? SnakePoints { get; set; }
    [JsonPropertyName("dojo_time_snake")] public int? SnakeTime { get; set; }

    [JsonPropertyName("dojo_points_fireball")] public int? FireballPoints { get; set; }
    [JsonPropertyName("dojo_time_fireball")] public int? FireballTime { get; set; }

    [JsonPropertyName("dojo_points_lock_head")] public int? LockHeadPoints { get; set; }
    [JsonPropertyName("dojo_time_lock_head")] public int? LockHeadTime { get; set; }
}

public class KuudraCompletedTiersResponse
{
    [JsonPropertyName("none")] public int? None { get; set; }
    [JsonPropertyName("hot")] public int? Hot { get; set; }
    [JsonPropertyName("burning")] public int? Burning { get; set; }
    [JsonPropertyName("fiery")] public int? Fiery { get; set; }
    [JsonPropertyName("infernal")] public int? Infernal { get; set; }
    [JsonPropertyName("highest_wave_none")] public int? HighestWaveNone { get; set; }
    [JsonPropertyName("highest_wave_hot")] public int? HighestWaveHot { get; set; }
    [JsonPropertyName("highest_wave_burning")] public int? HighestWaveBurning { get; set; }
    [JsonPropertyName("highest_wave_fiery")] public int? HighestWaveFiery { get; set; }
    [JsonPropertyName("highest_wave_infernal")] public int? HighestWaveInfernal { get; set; }
}

public class KuudraPartyFinderResponse
{
    [JsonPropertyName("search_settings")] public KuudraPartyFinderSearchSettings? SearchSettings { get; set; }
    [JsonPropertyName("group_builder")] public KuudraPartyFinderGroupBuilder? GroupBuilder { get; set; }
}

public class KuudraPartyFinderSearchSettings
{
    public string? Tier { get; set; }
    [JsonPropertyName("combat_level")] public string? CombatLevel { get; set; }
}

public class KuudraPartyFinderGroupBuilder
{
    public string? Tier { get; set; }
    public string? Note { get; set; }
    [JsonPropertyName("combat_level_required")] public int? CombatLevelRequired { get; set; }
}
