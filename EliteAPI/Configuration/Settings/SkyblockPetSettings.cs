namespace EliteAPI.Configuration.Settings;

public class SkyblockPetConfig {
	public static SkyblockPetSettings Settings { get; set; } = new();
}

public class SkyblockPetSettings {
	public Dictionary<string, int> MaxLevels { get; set; } = new();
	public Dictionary<string, int> RarityOffsets { get; set; } = new();
	public List<int> Levels { get; set; } = [];
}