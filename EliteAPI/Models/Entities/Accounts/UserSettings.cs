using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Models.Entities.Accounts;

public class UserSettings {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[MaxLength(16)]
	public string? Prefix { get; set; }
	[MaxLength(16)]
	public string? Suffix { get; set; }
	[MaxLength(256)]
	public string? EmojiUrl { get; set; }
	
	/// <summary>
	///	Selected weight image for the bot
	/// </summary>
	[Column(TypeName = "jsonb")]
	public ConfiguredProductFeatures Features { get; set; } = new();
	
	[ForeignKey(nameof(WeightStyle))]
	public int? WeightStyleId { get; set; }
	public WeightStyle? WeightStyle { get; set; }
	
	[ForeignKey(nameof(LeaderboardStyle))]
	public int? LeaderboardStyleId { get; set; }
	public WeightStyle? LeaderboardStyle { get; set; }
	
	/// <summary>
	/// Leaderboard custom cosmetics.
	/// </summary>
	[Column(TypeName = "jsonb")]
	public MemberLeaderboardCosmeticsDto? CustomLeaderboardStyle { get; set; }
}

public class ConfiguredProductFeatures {
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