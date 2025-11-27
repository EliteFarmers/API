using HypixelAPI.Networth.Models;

namespace EliteAPI.Features.Profiles.Models;

public class NetworthBreakdown
{
	public double Networth { get; set; }
	public double UnsoulboundNetworth { get; set; }
	public double Purse { get; set; }
	public double Bank { get; set; }
	public double PersonalBank { get; set; }

	public Dictionary<string, NetworthCategory> Categories { get; set; } = new();
}

public class NetworthCategory
{
	public double Total { get; set; }
	public double UnsoulboundTotal { get; set; }
	public List<NetworthResult> Items { get; set; } = new();
}