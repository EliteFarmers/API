using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HypixelAPI.DTOs;

public class ProfileMemberResponse
{
	[JsonPropertyName("player_data")]
	public RawMemberPlayerData? PlayerData { get; set; }
    
	[JsonPropertyName("pets_data")]
	public MemberPetsResponse? PetsData { get; set; }
    
	public MemberProfileDataResponse? Profile { get; set; }
    
	public RawMemberEvents? Events { get; set; }
    
	[JsonPropertyName("jacobs_contest")]
	public RawJacobData? Jacob { get; set; }
    
	public JsonDocument? Collection { get; set; }
    
	[JsonPropertyName("accessory_bag_storage")]
	public JsonObject? AccessoryBagSettings { get; set; }
	public JsonObject? Bestiary { get; set; }
    
	public RawLeveling? Leveling { get; set; }
    
	public MemberCurrenciesResponse? Currencies { get; set; }
    
	[JsonPropertyName("inventory")]
	public MemberInventoriesResponse? Inventories { get; set; }
	
	[JsonPropertyName("garden_player_data")]
	public GardenPlayerDataResponse? Garden { get; set; }
}

public class GardenPlayerDataResponse {
	public int Copper { get; set; }
	
	[JsonPropertyName("larva_consumed")]
	public int LarvaConsumed { get; set; }
}