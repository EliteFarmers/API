namespace HypixelAPI.Networth.Constants;

public static class NetworthConstants
{
    public static class ApplicationWorth
    {
        public const double Enrichment = 0.5;
        public const double FarmingForDummies = 0.5;
        public const double GemstonePowerScroll = 0.5;
        public const double WoodSingularity = 0.5;
        public const double ArtOfWar = 0.6;
        public const double FumingPotatoBook = 0.6;
        public const double GemstoneSlots = 0.6;
        public const double Runes = 0.6;
        public const double TunedTransmission = 0.7;
        public const double PocketSackInASack = 0.7;
        public const double Essence = 0.75;
        public const double Silex = 0.75;
        public const double ArtOfPeace = 0.8;
        public const double DivanPowderCoating = 0.8;
        public const double EnchantmentUpgrades = 0.8;
        public const double JalapenoBook = 0.8;
        public const double ManaDisintegrator = 0.8;
        public const double Recombobulator = 0.8;
        public const double ThunderInABottle = 0.8;
        public const double Enchantments = 0.85;
        public const double ShensAuctionPrice = 0.85;
        public const double Booster = 0.8;
        public const double Dye = 0.9;
        public const double GemstoneChambers = 0.9;
        public const double Attributes = 1.0;
        public const double RodPart = 1.0;
        public const double DrillPart = 1.0;
        public const double Etherwarp = 1.0;
        public const double MasterStar = 1.0;
        public const double Gemstone = 1.0;
        public const double HotPotatoBook = 1.0;
        public const double NecronBladeScroll = 1.0;
        public const double PolarvoidBook = 1.0;
        public const double PrestigeItem = 1.0;
        public const double Reforge = 1.0;
        public const double PetCandy = 0.65;
        public const double SoulboundPetSkins = 0.8;
        public const double SoulboundSkins = 0.8;
        public const double PetItem = 1.0;
    }

    public static readonly Dictionary<string, double> EnchantmentsWorth = new()
    {
        { "COUNTER_STRIKE", 0.2 },
        { "BIG_BRAIN", 0.35 },
        { "ULTIMATE_INFERNO", 0.35 },
        { "OVERLOAD", 0.35 },
        { "ULTIMATE_SOUL_EATER", 0.35 },
        { "ULTIMATE_FATAL_TEMPO", 0.65 }
    };

    public static readonly Dictionary<string, List<string>> BlockedEnchantments = new()
    {
        { "BONE_BOOMERANG", new List<string> { "OVERLOAD", "POWER", "ULTIMATE_SOUL_EATER" } },
        { "DEATH_BOW", new List<string> { "OVERLOAD", "POWER", "ULTIMATE_SOUL_EATER" } },
        { "GARDENING_AXE", new List<string> { "REPLENISH" } },
        { "GARDENING_HOE", new List<string> { "REPLENISH" } },
        { "ADVANCED_GARDENING_AXE", new List<string> { "REPLENISH" } },
        { "ADVANCED_GARDENING_HOE", new List<string> { "REPLENISH" } }
    };

    public static readonly Dictionary<string, int> IgnoredEnchantments = new()
    {
        { "SCAVENGER", 5 }
    };

    public static readonly HashSet<string> StackingEnchantments = new()
    {
        "EXPERTISE", "COMPACT", "ABSORB", "CULTIVATING", "CHAMPION", "HECATOMB", "TOXOPHILITE"
    };

    public static readonly HashSet<string> IgnoreSilex = new()
    {
        "PROMISING_SPADE", "PROMISING_AXE"
    };

    public static readonly Dictionary<string, string> Reforges = new()
    {
        { "stiff", "HARDENED_WOOD" },
        { "trashy", "OVERFLOWING_TRASH_CAN" },
        { "salty", "SALT_CUBE" },
        { "aote_stone", "AOTE_STONE" },
        { "blazing", "BLAZEN_SPHERE" },
        { "waxed", "BLAZE_WAX" },
        { "rooted", "BURROWING_SPORES" },
        { "calcified", "CALCIFIED_HEART" },
        { "candied", "CANDY_CORN" },
        { "perfect", "DIAMOND_ATOM" },
        { "fleet", "DIAMONITE" },
        { "fabled", "DRAGON_CLAW" },
        { "spiked", "DRAGON_SCALE" },
        { "royal", "DWARVEN_TREASURE" },
        { "hyper", "ENDSTONE_GEODE" },
        { "coldfusion", "ENTROPY_SUPPRESSOR" },
        { "blooming", "FLOWERING_BOUQUET" },
        { "fanged", "FULL_JAW_FANGING_KIT" },
        { "jaded", "JADERALD" },
        { "jerry", "JERRY_STONE" },
        { "magnetic", "LAPIS_CRYSTAL" },
        { "earthy", "LARGE_WALNUT" },
        { "groovy", "MANGROVE_GEM" },
        { "fortified", "METEOR_SHARD" },
        { "gilded", "MIDAS_JEWEL" },
        { "moonglade", "MOONGLADE_JEWEL" },
        { "cubic", "MOLTEN_CUBE" },
        { "necrotic", "NECROMANCER_BROOCH" },
        { "fruitful", "ONYX" },
        { "precise", "OPTICAL_LENS" },
        { "mossy", "OVERGROWN_GRASS" },
        { "pitchin", "PITCHIN_KOI" },
        { "undead", "PREMIUM_FLESH" },
        { "blood_soaked", "PRESUMED_GALLON_OF_RED_PAINT" },
        { "mithraic", "PURE_MITHRIL" },
        { "reinforced", "RARE_DIAMOND" },
        { "ridiculous", "RED_NOSE" },
        { "loving", "RED_SCARF" },
        { "auspicious", "ROCK_GEMSTONE" },
        { "treacherous", "RUSTY_ANCHOR" },
        { "headstrong", "SALMON_OPAL" },
        { "strengthened", "SEARING_STONE" },
        { "glistening", "SHINY_PRISM" },
        { "bustling", "SKYMART_BROCHURE" },
        { "spiritual", "SPIRIT_DECOY" },
        { "squeaky", "SQUEAKY_TOY" },
        { "suspicious", "SUSPICIOUS_VIAL" },
        { "snowy", "TERRY_SNOWGLOBE" },
        { "dimensional", "TITANIUM_TESSERACT" },
        { "ambered", "AMBER_MATERIAL" },
        { "beady", "BEADY_EYES" },
        { "blessed", "BLESSED_FRUIT" },
        { "bulky", "BULKY_STONE" },
        { "buzzing", "CLIPPED_WINGS" },
        { "submerged", "DEEP_SEA_ORB" },
        { "renowned", "DRAGON_HORN" },
        { "festive", "FROZEN_BAUBLE" },
        { "giant", "GIANT_TOOTH" },
        { "lustrous", "GLEAMING_CRYSTAL" },
        { "bountiful", "GOLDEN_BALL" },
        { "chomp", "KUUDRA_MANDIBLE" },
        { "lucky", "LUCKY_DICE" },
        { "stellar", "PETRIFIED_STARFALL" },
        { "scraped", "POCKET_ICEBERG" },
        { "ancient", "PRECURSOR_GEAR" },
        { "refined", "REFINED_AMBER" },
        { "empowered", "SADAN_BROOCH" },
        { "withered", "WITHER_BLOOD" },
        { "glacial", "FRIGID_HUSK" },
        { "heated", "HOT_STUFF" },
        { "dirty", "DIRT_BOTTLE" },
        { "moil", "MOIL_LOG" },
        { "toil", "TOIL_LOG" },
        { "greater_spook", "BOO_STONE" }
    };
    public static readonly List<string> MasterStars = new()
    {
        "FIRST_MASTER_STAR", "SECOND_MASTER_STAR", "THIRD_MASTER_STAR", "FOURTH_MASTER_STAR", "FIFTH_MASTER_STAR"
    };

    public static readonly List<string> AllowedRecombobulatedCategories = new()
    {
        "ACCESSORY", "NECKLACE", "GLOVES", "BRACELET", "BELT", "CLOAK", "VACUUM"
    };

    public static readonly List<string> AllowedRecombobulatedIds = new()
    {
        "DIVAN_HELMET",
        "DIVAN_CHESTPLATE",
        "DIVAN_LEGGINGS",
        "DIVAN_BOOTS",
        "FERMENTO_HELMET",
        "FERMENTO_CHESTPLATE",
        "FERMENTO_LEGGINGS",
        "FERMENTO_BOOTS",
        "SHADOW_ASSASSIN_CLOAK",
        "STARRED_SHADOW_ASSASSIN_CLOAK"
    };

    public static readonly List<string> Enrichments = new()
    {
        "TALISMAN_ENRICHMENT_CRITICAL_CHANCE",
        "TALISMAN_ENRICHMENT_CRITICAL_DAMAGE",
        "TALISMAN_ENRICHMENT_DEFENSE",
        "TALISMAN_ENRICHMENT_HEALTH",
        "TALISMAN_ENRICHMENT_INTELLIGENCE",
        "TALISMAN_ENRICHMENT_MAGIC_FIND",
        "TALISMAN_ENRICHMENT_WALK_SPEED",
        "TALISMAN_ENRICHMENT_STRENGTH",
        "TALISMAN_ENRICHMENT_ATTACK_SPEED",
        "TALISMAN_ENRICHMENT_FEROCITY",
        "TALISMAN_ENRICHMENT_SEA_CREATURE_CHANCE"
    };

    public static readonly Dictionary<string, string> SpecialEnchantmentNames = new()
    {
        { "aiming", "Dragon Tracer" },
        { "pristine", "Prismatic" },
        { "counter_strike", "Counter-Strike" },
        { "turbo_cacti", "Turbo-Cacti" },
        { "turbo_cane", "Turbo-Cane" },
        { "turbo_carrot", "Turbo-Carrot" },
        { "turbo_cocoa", "Turbo-Cocoa" },
        { "turbo_melon", "Turbo-Melon" },
        { "turbo_mushrooms", "Turbo-Mushrooms" },
        { "turbo_potato", "Turbo-Potato" },
        { "turbo_pumpkin", "Turbo-Pumpkin" },
        { "turbo_warts", "Turbo-Warts" },
        { "turbo_wheat", "Turbo-Wheat" },
        { "ultimate_reiterate", "Ultimate Duplex" },
        { "ultimate_bobbin_time", "Ultimate Bobbin' Time" },
        { "arcane", "Woodsplitter" },
        { "dragon_hunter", "Gravity" }
    };

    public static readonly List<string> GemstoneSlots = new()
    {
        "COMBAT", "OFFENSIVE", "DEFENSIVE", "MINING", "UNIVERSAL", "CHISEL"
    };

    public static readonly HashSet<string> NonCosmeticItems = new()
    {
        "ANCIENT_ELEVATOR",
        "BEDROCK",
        "CREATIVE_MIND",
        "DCTR_SPACE_HELM",
        "DEAD_BUSH_OF_LOVE",
        "DUECES_BUILDER_CLAY",
        "GAME_BREAKER",
        "POTATO_BASKET"
    };

    public static readonly Dictionary<string, List<string>> Prestiges = new()
    {
        { "HOT_CRIMSON_CHESTPLATE", new List<string> { "CRIMSON_CHESTPLATE" } },
        { "HOT_CRIMSON_HELMET", new List<string> { "CRIMSON_HELMET" } },
        { "HOT_CRIMSON_LEGGINGS", new List<string> { "CRIMSON_LEGGINGS" } },
        { "HOT_CRIMSON_BOOTS", new List<string> { "CRIMSON_BOOTS" } },
        { "BURNING_CRIMSON_CHESTPLATE", new List<string> { "HOT_CRIMSON_CHESTPLATE", "CRIMSON_CHESTPLATE" } },
        { "BURNING_CRIMSON_HELMET", new List<string> { "HOT_CRIMSON_HELMET", "CRIMSON_HELMET" } },
        { "BURNING_CRIMSON_LEGGINGS", new List<string> { "HOT_CRIMSON_LEGGINGS", "CRIMSON_LEGGINGS" } },
        { "BURNING_CRIMSON_BOOTS", new List<string> { "HOT_CRIMSON_BOOTS", "CRIMSON_BOOTS" } },
        { "FIERY_CRIMSON_CHESTPLATE", new List<string> { "BURNING_CRIMSON_CHESTPLATE", "HOT_CRIMSON_CHESTPLATE", "CRIMSON_CHESTPLATE" } },
        { "FIERY_CRIMSON_HELMET", new List<string> { "BURNING_CRIMSON_HELMET", "HOT_CRIMSON_HELMET", "CRIMSON_HELMET" } },
        { "FIERY_CRIMSON_LEGGINGS", new List<string> { "BURNING_CRIMSON_LEGGINGS", "HOT_CRIMSON_LEGGINGS", "CRIMSON_LEGGINGS" } },
        { "FIERY_CRIMSON_BOOTS", new List<string> { "BURNING_CRIMSON_BOOTS", "HOT_CRIMSON_BOOTS", "CRIMSON_BOOTS" } },
        { "INFERNAL_CRIMSON_CHESTPLATE", new List<string> { "FIERY_CRIMSON_CHESTPLATE", "BURNING_CRIMSON_CHESTPLATE", "HOT_CRIMSON_CHESTPLATE", "CRIMSON_CHESTPLATE" } },
        { "INFERNAL_CRIMSON_HELMET", new List<string> { "FIERY_CRIMSON_HELMET", "BURNING_CRIMSON_HELMET", "HOT_CRIMSON_HELMET", "CRIMSON_HELMET" } },
        { "INFERNAL_CRIMSON_LEGGINGS", new List<string> { "FIERY_CRIMSON_LEGGINGS", "BURNING_CRIMSON_LEGGINGS", "HOT_CRIMSON_LEGGINGS", "CRIMSON_LEGGINGS" } },
        { "INFERNAL_CRIMSON_BOOTS", new List<string> { "FIERY_CRIMSON_BOOTS", "BURNING_CRIMSON_BOOTS", "HOT_CRIMSON_BOOTS", "CRIMSON_BOOTS" } },
        { "HOT_TERROR_CHESTPLATE", new List<string> { "TERROR_CHESTPLATE" } },
        { "HOT_TERROR_HELMET", new List<string> { "TERROR_HELMET" } },
        { "HOT_TERROR_LEGGINGS", new List<string> { "TERROR_LEGGINGS" } },
        { "HOT_TERROR_BOOTS", new List<string> { "TERROR_BOOTS" } },
        { "BURNING_TERROR_CHESTPLATE", new List<string> { "HOT_TERROR_CHESTPLATE", "TERROR_CHESTPLATE" } },
        { "BURNING_TERROR_HELMET", new List<string> { "HOT_TERROR_HELMET", "TERROR_HELMET" } },
        { "BURNING_TERROR_LEGGINGS", new List<string> { "HOT_TERROR_LEGGINGS", "TERROR_LEGGINGS" } },
        { "BURNING_TERROR_BOOTS", new List<string> { "HOT_TERROR_BOOTS", "TERROR_BOOTS" } },
        { "FIERY_TERROR_CHESTPLATE", new List<string> { "BURNING_TERROR_CHESTPLATE", "HOT_TERROR_CHESTPLATE", "TERROR_CHESTPLATE" } },
        { "FIERY_TERROR_HELMET", new List<string> { "BURNING_TERROR_HELMET", "HOT_TERROR_HELMET", "TERROR_HELMET" } },
        { "FIERY_TERROR_LEGGINGS", new List<string> { "BURNING_TERROR_LEGGINGS", "HOT_TERROR_LEGGINGS", "TERROR_LEGGINGS" } },
        { "FIERY_TERROR_BOOTS", new List<string> { "BURNING_TERROR_BOOTS", "HOT_TERROR_BOOTS", "TERROR_BOOTS" } },
        { "INFERNAL_TERROR_CHESTPLATE", new List<string> { "FIERY_TERROR_CHESTPLATE", "BURNING_TERROR_CHESTPLATE", "HOT_TERROR_CHESTPLATE", "TERROR_CHESTPLATE" } },
        { "INFERNAL_TERROR_HELMET", new List<string> { "FIERY_TERROR_HELMET", "BURNING_TERROR_HELMET", "HOT_TERROR_HELMET", "TERROR_HELMET" } },
        { "INFERNAL_TERROR_LEGGINGS", new List<string> { "FIERY_TERROR_LEGGINGS", "BURNING_TERROR_LEGGINGS", "HOT_TERROR_LEGGINGS", "TERROR_LEGGINGS" } },
        { "INFERNAL_TERROR_BOOTS", new List<string> { "FIERY_TERROR_BOOTS", "BURNING_TERROR_BOOTS", "HOT_TERROR_BOOTS", "TERROR_BOOTS" } },
        { "HOT_FERVOR_CHESTPLATE", new List<string> { "FERVOR_CHESTPLATE" } },
        { "HOT_FERVOR_HELMET", new List<string> { "FERVOR_HELMET" } },
        { "HOT_FERVOR_LEGGINGS", new List<string> { "FERVOR_LEGGINGS" } },
        { "HOT_FERVOR_BOOTS", new List<string> { "FERVOR_BOOTS" } },
        { "BURNING_FERVOR_CHESTPLATE", new List<string> { "HOT_FERVOR_CHESTPLATE", "FERVOR_CHESTPLATE" } },
        { "BURNING_FERVOR_HELMET", new List<string> { "HOT_FERVOR_HELMET", "FERVOR_HELMET" } },
        { "BURNING_FERVOR_LEGGINGS", new List<string> { "HOT_FERVOR_LEGGINGS", "FERVOR_LEGGINGS" } },
        { "BURNING_FERVOR_BOOTS", new List<string> { "HOT_FERVOR_BOOTS", "FERVOR_BOOTS" } },
        { "FIERY_FERVOR_CHESTPLATE", new List<string> { "BURNING_FERVOR_CHESTPLATE", "HOT_FERVOR_CHESTPLATE", "FERVOR_CHESTPLATE" } },
        { "FIERY_FERVOR_HELMET", new List<string> { "BURNING_FERVOR_HELMET", "HOT_FERVOR_HELMET", "FERVOR_HELMET" } },
        { "FIERY_FERVOR_LEGGINGS", new List<string> { "BURNING_FERVOR_LEGGINGS", "HOT_FERVOR_LEGGINGS", "FERVOR_LEGGINGS" } },
        { "FIERY_FERVOR_BOOTS", new List<string> { "BURNING_FERVOR_BOOTS", "HOT_FERVOR_BOOTS", "FERVOR_BOOTS" } },
        { "INFERNAL_FERVOR_CHESTPLATE", new List<string> { "FIERY_FERVOR_CHESTPLATE", "BURNING_FERVOR_CHESTPLATE", "HOT_FERVOR_CHESTPLATE", "FERVOR_CHESTPLATE" } },
        { "INFERNAL_FERVOR_HELMET", new List<string> { "FIERY_FERVOR_HELMET", "BURNING_FERVOR_HELMET", "HOT_FERVOR_HELMET", "FERVOR_HELMET" } },
        { "INFERNAL_FERVOR_LEGGINGS", new List<string> { "FIERY_FERVOR_LEGGINGS", "BURNING_FERVOR_LEGGINGS", "HOT_FERVOR_LEGGINGS", "FERVOR_LEGGINGS" } },
        { "INFERNAL_FERVOR_BOOTS", new List<string> { "FIERY_FERVOR_BOOTS", "BURNING_FERVOR_BOOTS", "HOT_FERVOR_BOOTS", "FERVOR_BOOTS" } },
        { "HOT_HOLLOW_CHESTPLATE", new List<string> { "HOLLOW_CHESTPLATE" } },
        { "HOT_HOLLOW_HELMET", new List<string> { "HOLLOW_HELMET" } },
        { "HOT_HOLLOW_LEGGINGS", new List<string> { "HOLLOW_LEGGINGS" } },
        { "HOT_HOLLOW_BOOTS", new List<string> { "HOLLOW_BOOTS" } },
        { "BURNING_HOLLOW_CHESTPLATE", new List<string> { "HOT_HOLLOW_CHESTPLATE", "HOLLOW_CHESTPLATE" } },
        { "BURNING_HOLLOW_HELMET", new List<string> { "HOT_HOLLOW_HELMET", "HOLLOW_HELMET" } },
        { "BURNING_HOLLOW_LEGGINGS", new List<string> { "HOT_HOLLOW_LEGGINGS", "HOLLOW_LEGGINGS" } },
        { "BURNING_HOLLOW_BOOTS", new List<string> { "HOT_HOLLOW_BOOTS", "HOLLOW_BOOTS" } },
        { "FIERY_HOLLOW_CHESTPLATE", new List<string> { "BURNING_HOLLOW_CHESTPLATE", "HOT_HOLLOW_CHESTPLATE", "HOLLOW_CHESTPLATE" } },
        { "FIERY_HOLLOW_HELMET", new List<string> { "BURNING_HOLLOW_HELMET", "HOT_HOLLOW_HELMET", "HOLLOW_HELMET" } },
        { "FIERY_HOLLOW_LEGGINGS", new List<string> { "BURNING_HOLLOW_LEGGINGS", "HOT_HOLLOW_LEGGINGS", "HOLLOW_LEGGINGS" } },
        { "FIERY_HOLLOW_BOOTS", new List<string> { "BURNING_HOLLOW_BOOTS", "HOT_HOLLOW_BOOTS", "HOLLOW_BOOTS" } },
        { "INFERNAL_HOLLOW_CHESTPLATE", new List<string> { "FIERY_HOLLOW_CHESTPLATE", "BURNING_HOLLOW_CHESTPLATE", "HOT_HOLLOW_CHESTPLATE", "HOLLOW_CHESTPLATE" } },
        { "INFERNAL_HOLLOW_HELMET", new List<string> { "FIERY_HOLLOW_HELMET", "BURNING_HOLLOW_HELMET", "HOT_HOLLOW_HELMET", "HOLLOW_HELMET" } },
        { "INFERNAL_HOLLOW_LEGGINGS", new List<string> { "FIERY_HOLLOW_LEGGINGS", "BURNING_HOLLOW_LEGGINGS", "HOT_HOLLOW_LEGGINGS", "HOLLOW_LEGGINGS" } },
        { "INFERNAL_HOLLOW_BOOTS", new List<string> { "FIERY_HOLLOW_BOOTS", "BURNING_HOLLOW_BOOTS", "HOT_HOLLOW_BOOTS", "HOLLOW_BOOTS" } },
        { "HOT_AURORA_CHESTPLATE", new List<string> { "AURORA_CHESTPLATE" } },
        { "HOT_AURORA_HELMET", new List<string> { "AURORA_HELMET" } },
        { "HOT_AURORA_LEGGINGS", new List<string> { "AURORA_LEGGINGS" } },
        { "HOT_AURORA_BOOTS", new List<string> { "AURORA_BOOTS" } },
        { "BURNING_AURORA_CHESTPLATE", new List<string> { "HOT_AURORA_CHESTPLATE", "AURORA_CHESTPLATE" } },
        { "BURNING_AURORA_HELMET", new List<string> { "HOT_AURORA_HELMET", "AURORA_HELMET" } },
        { "BURNING_AURORA_LEGGINGS", new List<string> { "HOT_AURORA_LEGGINGS", "AURORA_LEGGINGS" } },
        { "BURNING_AURORA_BOOTS", new List<string> { "HOT_AURORA_BOOTS", "AURORA_BOOTS" } },
        { "FIERY_AURORA_CHESTPLATE", new List<string> { "BURNING_AURORA_CHESTPLATE", "HOT_AURORA_CHESTPLATE", "AURORA_CHESTPLATE" } },
        { "FIERY_AURORA_HELMET", new List<string> { "BURNING_AURORA_HELMET", "HOT_AURORA_HELMET", "AURORA_HELMET" } },
        { "FIERY_AURORA_LEGGINGS", new List<string> { "BURNING_AURORA_LEGGINGS", "HOT_AURORA_LEGGINGS", "AURORA_LEGGINGS" } },
        { "FIERY_AURORA_BOOTS", new List<string> { "BURNING_AURORA_BOOTS", "HOT_AURORA_BOOTS", "AURORA_BOOTS" } },
        { "INFERNAL_AURORA_CHESTPLATE", new List<string> { "FIERY_AURORA_CHESTPLATE", "BURNING_AURORA_CHESTPLATE", "HOT_AURORA_CHESTPLATE", "AURORA_CHESTPLATE" } },
        { "INFERNAL_AURORA_HELMET", new List<string> { "FIERY_AURORA_HELMET", "BURNING_AURORA_HELMET", "HOT_AURORA_HELMET", "AURORA_HELMET" } },
        { "INFERNAL_AURORA_LEGGINGS", new List<string> { "FIERY_AURORA_LEGGINGS", "BURNING_AURORA_LEGGINGS", "HOT_AURORA_LEGGINGS", "AURORA_LEGGINGS" } },
        { "INFERNAL_AURORA_BOOTS", new List<string> { "FIERY_AURORA_BOOTS", "BURNING_AURORA_BOOTS", "HOT_AURORA_BOOTS", "AURORA_BOOTS" } }
    };

    public static readonly List<string> BlockedCandyReducePets = new()
    {
        "ENDER_DRAGON", "GOLDEN_DRAGON", "SCATHA"
    };

    public static readonly Dictionary<string, int> SpecialLevels = new()
    {
        { "GOLDEN_DRAGON", 200 },
        { "JADE_DRAGON", 200 }
    };

    public static readonly Dictionary<string, int> RarityOffset = new()
    {
        { "COMMON", 0 },
        { "UNCOMMON", 6 },
        { "RARE", 11 },
        { "EPIC", 16 },
        { "LEGENDARY", 20 },
        { "MYTHIC", 20 }
    };

    public static readonly List<string> Tiers = new()
    {
        "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY", "MYTHIC", "DIVINE", "SPECIAL", "VERY_SPECIAL", "ULTIMATE"
    };

    public static readonly List<int> Levels = new()
    {
        100, 110, 120, 130, 145, 160, 175, 190, 210, 230, 250, 275, 300, 330, 360, 400, 440, 490, 540, 600, 660, 730, 800, 880, 960, 1050, 1150, 1260, 1380, 1510, 1650, 1800,
        1960, 2130, 2310, 2500, 2700, 2920, 3160, 3420, 3700, 4000, 4350, 4750, 5200, 5700, 6300, 7000, 7800, 8700, 9700, 10800, 12000, 13300, 14700, 16200, 17800, 19500,
        21300, 23200, 25200, 27400, 29800, 32400, 35200, 38200, 41400, 44800, 48400, 52200, 56200, 60400, 64800, 69400, 74200, 79200, 84700, 90700, 97200, 104200, 111700,
        119700, 128200, 137200, 146700, 156700, 167700, 179700, 192700, 206700, 221700, 237700, 254700, 272700, 291700, 311700, 333700, 357700, 383700, 411700, 441700,
        476700, 516700, 561700, 611700, 666700, 726700, 791700, 861700, 936700, 1016700, 1101700, 1191700, 1286700, 1386700, 1496700, 1616700, 1746700, 1886700, 0, 5555,
        1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700,
        1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700,
        1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700,
        1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700,
        1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700,
        1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700, 1886700
    };
}
