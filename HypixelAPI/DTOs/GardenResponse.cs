using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class GardenResponse {
	public bool Success { get; set; }
	public GardenResponseData? Garden { get; set; }
}

public class GardenResponseData {
	/// <summary>
	/// Profile ID
	/// </summary>
	[JsonPropertyName("uuid")]
	public required string ProfileId { get; set; }
	
	/// <summary>
	/// Garden experience
	/// </summary>
	[JsonPropertyName("garden_experience")]
	public double GardenExperience { get; set; }
	
	/// <summary>
	/// List of plot ids that are unlocked
	/// </summary>
	[JsonPropertyName("unlocked_plots_ids")]
	public List<string> UnlockedPlots { get; set; } = new();
	
	/// <summary>
	/// Visitor data
	/// </summary>
	[JsonPropertyName("commission_data")]
	public GardenVisitorData? Visitors { get; set; } = new();
	
	/// <summary>
	/// Crop collections counted towards milestones
	/// </summary>
	[JsonPropertyName("resources_collected")]
	public Dictionary<string, long> CropMilestones { get; set; } = new();
	
	/// <summary>
	/// Crop upgrade levels
	/// </summary>
	[JsonPropertyName("crop_upgrade_levels")]
	public Dictionary<string, short> CropUpgrades { get; set; } = new();
	
	/// <summary>
	/// Compster data
	/// </summary>
	[JsonPropertyName("composter_data")]
	public ComposterData? Composter { get; set; } = new();
	
	/// <summary>
	/// Selected barn skin
	/// </summary>
	[JsonPropertyName("selected_barn_skin")]
	public string? SelectedBarnSkin { get; set; } 
	
	/// <summary>
	/// Visitors currently on the garden
	/// </summary>
	[JsonPropertyName("active_commissions")]
	public Dictionary<string, object> CurrentVisitors { get; set; } = new();
}

public class GardenVisitorData {
	/// <summary>
	/// The amount of visits each visitor has made to the garden
	/// </summary>
	public Dictionary<string, int> Visits { get; set; } = new();
	
	/// <summary>
	/// The amount of accepted trades each visitor has
	/// </summary>
	public Dictionary<string, int> Completed { get; set; } = new();
	
	/// <summary>
	/// Total amount of accepted trades
	/// </summary>
	[JsonPropertyName("total_completed")]
	public int TotalVisitorsServed { get; set; }
	
	/// <summary>
	/// Total amount of unique visitors accepted
	/// </summary>
	[JsonPropertyName("unique_npcs_served")]
	public int UniqueVisitorsServed { get; set; }
}

public class ComposterData {
	/// <summary>
	/// Organic matter currently in the composter
	/// </summary>
	[JsonPropertyName("organic_matter")]
	public double OrganicMatter { get; set; }
	
	/// <summary>
	/// Amount of fuel in the composter
	/// </summary>
	[JsonPropertyName("fuel_units")]
	public double FuelUnits { get; set; }
	
	/// <summary>
	/// Compost units?
	/// </summary>
	[JsonPropertyName("compost_units")]
	public int CompostAvailable { get; set; }
	
	/// <summary>
	/// Amount of compost items currently claimable in the composter
	/// </summary>
	[JsonPropertyName("compost_items")]
	public int Compost { get; set; }
	
	/// <summary>
	/// Amount of seconds it takes to convert organic matter into 1 compost item
	/// </summary>
	[JsonPropertyName("conversion_ticks")]
	public int ConversionSeconds { get; set; }
	
	/// <summary>
	/// Unix timestamp of the last time the composter was saved in milliseconds
	/// </summary>
	[JsonPropertyName("last_save")]
	public long LastSave { get; set; }
	
	/// <summary>
	/// Composter upgrades
	/// </summary>
	public ComposterUpgrades Upgrades { get; set; } = new();
}

public class ComposterUpgrades {
	/// <summary>
	/// Composter speed upgrade
	/// </summary>
	public int Speed { get; set; }
	
	/// <summary>
	/// Composter multi-drop upgrade
	/// </summary>
	[JsonPropertyName("multi_drop")]
	public int MultiDrop { get; set; }
	
	/// <summary>
	/// Composter fuel capacity upgrade
	/// </summary>
	[JsonPropertyName("fuel_cap")]
	public int FuelCap { get; set; }
	
	/// <summary>
	/// Composter organic matter capacity upgrade
	/// </summary>
	[JsonPropertyName("organic_matter_cap")]
	public int OrganicMatterCap { get; set; }
	
	/// <summary>
	/// Composter cost reduction upgrade
	/// </summary>
	[JsonPropertyName("cost_reduction")]
	public int CostReduction { get; set; }
}