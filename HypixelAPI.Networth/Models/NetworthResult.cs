namespace HypixelAPI.Networth.Models;

public class NetworthResult
{
	public NetworthItemSimple? Item { get; set; }
	public double BasePrice { get; set; }
	public double Price { get; set; }
	public double Networth { get; set; }
	public List<NetworthCalculation>? Calculation { get; set; }
	public bool Soulbound { get; set; }
	public bool Cosmetic { get; set; }
	public double LiquidNetworth { get; set; }
	public double FunctionalNetworth { get; set; }
	public double LiquidFunctionalNetworth { get; set; }
	public double CosmeticValue { get; set; }
	public double IlliquidValue { get; set; }
}

public class NetworthCalculation
{
	public string Id { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public double Value { get; set; }
	public int Count { get; set; }
}