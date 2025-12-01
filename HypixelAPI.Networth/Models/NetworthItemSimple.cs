using System.Text.Json.Serialization;

namespace HypixelAPI.Networth.Models;

public class NetworthItemSimple
{
	public string? SkyblockId { get; set; }
	public string? Name { get; set; }
	public string? Slot { get; set; }
	public int Count { get; set; }
	public short Damage { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Uuid { get; set; }
}
