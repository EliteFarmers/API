namespace EliteAPI.Features.Account.DTOs;

public class FortuneSettingsDto {
	/// <summary>
	/// Member fortune settings for each minecraft account, then each profile.
	/// </summary>
	public Dictionary<string, Dictionary<string, MemberFortuneSettingsDto>> Accounts { get; set; } = new();
}

public class MemberFortuneSettingsDto {
	/// <summary>
	/// Amount of strength used for mooshroom fortune
	/// </summary>
	public int Strength { get; set; } = 0;

	/// <summary>
	/// Community center farming fortune level
	/// </summary>
	public int CommunityCenter { get; set; } = 0;

	/// <summary>
	/// Attribute shards
	/// </summary>
	public Dictionary<string, int> Attributes { get; set; } = new();

	/// <summary>
	/// Exported crops
	/// </summary>
	public Dictionary<string, bool> Exported { get; set; } = new();
}