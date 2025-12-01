using HypixelAPI.Networth.Models;

namespace EliteAPI.Features.Profiles.Models;

public class NetworthBreakdown
{
	public double Networth { get; set; }
	public double LiquidNetworth { get; set; }
	public double FunctionalNetworth { get; set; }
	public double LiquidFunctionalNetworth { get; set; }
	public double Purse { get; set; }
	public double Bank { get; set; }
	public double PersonalBank { get; set; }

	public Dictionary<string, NetworthCategory> Categories { get; set; } = new();
}

public class NetworthCategory
{
	public double Total { get; set; }
	public double LiquidTotal { get; set; }
	public double NonCosmeticTotal { get; set; }
	public double LiquidFunctionalTotal { get; set; }
	public List<NetworthResult> Items { get; set; } = new();
}