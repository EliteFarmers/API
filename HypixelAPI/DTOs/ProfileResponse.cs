using System.Text.Json.Serialization;

namespace HypixelAPI.DTOs;

public class ProfileResponse
{
	public bool Selected { get; set; } = false;

	[JsonPropertyName("cute_name")]
	public required string CuteName { get; set; }

	[JsonPropertyName("profile_id")]
	public required string ProfileId { get; set; }

	[JsonPropertyName("game_mode")]
	public string? GameMode { get; set; }

	[JsonPropertyName("community_upgrades")]
	public ProfileCommunityUpgrades? CommunityUpgrades { get; set; }

	[JsonPropertyName("last_save")]
	public long LastSave { get; set; }
	
	public ProfileBankingResponse? Banking { get; set; }
	
	public required Dictionary<string, ProfileMemberResponse> Members { get; set; }
}