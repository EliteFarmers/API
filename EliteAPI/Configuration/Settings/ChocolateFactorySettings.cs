namespace EliteAPI.Configuration.Settings;

public class ChocolateFactorySettings
{
	public RabbitSettings Rabbits { get; set; } = new();
	public Dictionary<string, RabbitRaritySettings> RabbitOverrides { get; set; } = new();
}

public class RabbitSettings
{
	public RabbitRaritySettings Common { get; set; } = new();
	public RabbitRaritySettings Uncommon { get; set; } = new();
	public RabbitRaritySettings Rare { get; set; } = new();
	public RabbitRaritySettings Epic { get; set; } = new();
	public RabbitRaritySettings Legendary { get; set; } = new();
	public RabbitRaritySettings Mythic { get; set; } = new();
	public RabbitRaritySettings Divine { get; set; } = new();
}

public class RabbitRaritySettings
{
	public List<string> Rabbits { get; set; } = [];
	public int Chocolate { get; set; }
	public double Multiplier { get; set; }
}

public class RabbitOverrideSettings
{
	public int Chocolate { get; set; }
	public double Multiplier { get; set; }
}