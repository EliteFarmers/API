using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class RawMiningCoreResponse
{
    // Common HOTM/mining fields observed in samples
    [JsonPropertyName("received_free_tier")] public bool? ReceivedFreeTier { get; set; }
    [JsonPropertyName("hotm_migrator_tree_reset_send_message")] public bool? HotmMigratorTreeResetSendMessage { get; set; }

    public int? Tokens { get; set; }

    // Mithril powder
    [JsonPropertyName("powder_mithril")] public long? PowderMithril { get; set; }
    [JsonPropertyName("powder_mithril_total")] public long? PowderMithrilTotal { get; set; }
    [JsonPropertyName("powder_spent_mithril")] public long? PowderSpentMithril { get; set; }
    [JsonPropertyName("daily_ores_mined_day_mithril_ore")] public int? DailyOresMinedDayMithrilOre { get; set; }
    [JsonPropertyName("daily_ores_mined_mithril_ore")] public int? DailyOresMinedMithrilOre { get; set; }

    // Gemstone powder
    [JsonPropertyName("powder_gemstone")] public long? PowderGemstone { get; set; }
    [JsonPropertyName("powder_gemstone_total")] public long? PowderGemstoneTotal { get; set; }
    [JsonPropertyName("powder_spent_gemstone")] public long? PowderSpentGemstone { get; set; }
    [JsonPropertyName("daily_ores_mined_day_gemstone")] public int? DailyOresMinedDayGemstone { get; set; }
    [JsonPropertyName("daily_ores_mined_gemstone")] public int? DailyOresMinedGemstone { get; set; }

    // Glacite powder
    [JsonPropertyName("powder_glacite")] public long? PowderGlacite { get; set; }
    [JsonPropertyName("powder_glacite_total")] public long? PowderGlaciteTotal { get; set; }
    [JsonPropertyName("powder_spent_glacite")] public long? PowderSpentGlacite { get; set; }
    [JsonPropertyName("daily_ores_mined_day_glacite")] public int? DailyOresMinedDayGlacite { get; set; }
    [JsonPropertyName("daily_ores_mined_glacite")] public int? DailyOresMinedGlacite { get; set; }

    // Daily ores totals
    [JsonPropertyName("daily_ores_mined")] public int? DailyOresMined { get; set; }
    [JsonPropertyName("daily_ores_mined_day")] public int? DailyOresMinedDay { get; set; }

    // Misc HOTM flags
    [JsonPropertyName("retroactive_tier2_token")] public bool? RetroactiveTier2Token { get; set; }

    // Crystal Hollows crystals and biomes
    public MiningCoreCrystals? Crystals { get; set; }
    public MiningCoreBiomes? Biomes { get; set; }

    [JsonPropertyName("greater_mines_last_access")] public long? GreaterMinesLastAccess { get; set; }
}

public class MiningCoreCrystals
{
    [JsonPropertyName("jade_crystal")] public MiningCrystalInfo? Jade { get; set; }
    [JsonPropertyName("amber_crystal")] public MiningCrystalInfo? Amber { get; set; }
    [JsonPropertyName("amethyst_crystal")] public MiningCrystalInfo? Amethyst { get; set; }
    [JsonPropertyName("sapphire_crystal")] public MiningCrystalInfo? Sapphire { get; set; }
    [JsonPropertyName("topaz_crystal")] public MiningCrystalInfo? Topaz { get; set; }
    [JsonPropertyName("jasper_crystal")] public MiningCrystalInfo? Jasper { get; set; }
    [JsonPropertyName("ruby_crystal")] public MiningCrystalInfo? Ruby { get; set; }
    [JsonPropertyName("aquamarine_crystal")] public MiningCrystalInfo? Aquamarine { get; set; }
    [JsonPropertyName("citrine_crystal")] public MiningCrystalInfo? Citrine { get; set; }
    [JsonPropertyName("peridot_crystal")] public MiningCrystalInfo? Peridot { get; set; }
    [JsonPropertyName("onyx_crystal")] public MiningCrystalInfo? Onyx { get; set; }
    [JsonPropertyName("opal_crystal")] public MiningCrystalInfo? Opal { get; set; }
}

public class MiningCrystalInfo
{
    public string? State { get; set; }
    [JsonPropertyName("total_placed")] public int? TotalPlaced { get; set; }
    [JsonPropertyName("total_found")] public int? TotalFound { get; set; }
}

public class MiningCoreBiomes
{
    public MiningBiomeDwarven? Dwarven { get; set; }
    public MiningBiomePrecursor? Precursor { get; set; }
    public MiningBiomeGoblin? Goblin { get; set; }
    public MiningBiomeJungle? Jungle { get; set; }
}

public class MiningBiomeDwarven { }

public class MiningBiomePrecursor { }

public class MiningBiomeGoblin
{
    [JsonPropertyName("king_quest_active")] public bool? KingQuestActive { get; set; }
    [JsonPropertyName("king_quests_completed")] public int? KingQuestsCompleted { get; set; }
}

public class MiningBiomeJungle
{
    [JsonPropertyName("jungle_temple_open")] public bool? JungleTempleOpen { get; set; }
    [JsonPropertyName("jungle_temple_chest_uses")] public int? JungleTempleChestUses { get; set; }
}
