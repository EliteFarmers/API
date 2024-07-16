using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Accounts;

public class UserSettings {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	/// <summary>
	///	Selected weight image for the bot
	/// </summary>
	[Column(TypeName = "jsonb")]
	public ConfiguredProductFeatures Features { get; set; } = new();
}

public class ConfiguredProductFeatures {
	/// <summary>
	///	Selected weight image for the bot
	/// </summary>
	public string? WeightStyle { get; set; }
	/// <summary>
	/// Embed color for the bot
	/// </summary>
	public string? EmbedColor { get; set; }
	/// <summary>
	/// If other's weight styles can be overridden
	/// </summary>
	public bool WeightStyleOverride { get; set; }
	/// <summary>
	/// Hide shop promotions
	/// </summary>
	public bool HideShopPromotions { get; set; }
	/// <summary>
	/// Show "More Info" on weight command by default
	/// </summary>
	public bool MoreInfoDefault { get; set; }
}