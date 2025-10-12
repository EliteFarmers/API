using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Models.DTOs.Outgoing;

public class GardenDto {
	/// <summary>
	/// Profile ID
	/// </summary>
	public required string ProfileId { get; set; }

	/// <summary>
	/// Garden experience
	/// </summary>
	public int Experience { get; set; } = 0;

	/// <summary>
	/// Total completed visitors
	/// </summary>
	public int CompletedVisitors { get; set; } = 0;

	/// <summary>
	/// Unique visitors unlocked
	/// </summary>
	public int UniqueVisitors { get; set; } = 0;

	/// <summary>
	/// Crops counted towards milestones
	/// </summary>
	public CropSettings<string> Crops { get; set; } = new();

	/// <summary>
	/// Crop upgrades
	/// </summary>
	public CropSettings<int> CropUpgrades { get; set; } = new();

	/// <summary>
	/// List of unlocked plots
	/// </summary>
	public List<string> Plots { get; set; } = [];

	/// <summary>
	/// Composter data
	/// </summary>
	public ComposterDto Composter { get; set; } = new();

	/// <summary>
	/// Visitor data
	/// </summary>
	public Dictionary<string, VisitorDto> Visitors { get; set; } = new();

	/// <summary>
	/// Last save time in unix seconds
	/// </summary>
	public string LastSave { get; set; } = "0";
}

/// <inheritdoc />
public class ComposterDto : ComposterData {
	/// <summary>
	/// Last save time in unix seconds
	/// </summary>
	public new long LastSave { get; set; }
}

/// <inheritdoc />
public class VisitorDto : VisitorData;