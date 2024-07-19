using HypixelAPI.DTOs;
using Refit;

namespace HypixelAPI;

[Headers("Content-Type: application/json")]
public interface IHypixelApi {
	public const string BaseHypixelUrl = "https://api.hypixel.net/v2";
	
	[Get("/skyblock/profiles?uuid={uuid}")]
	Task<ApiResponse<ProfilesResponse>> FetchProfiles(string uuid);
	
	[Get("/player?uuid={uuid}")]
	Task<ApiResponse<PlayerResponse>> FetchPlayer(string uuid);
	
	[Get("/skyblock/garden?profile={profileId}")]
	Task<ApiResponse<GardenResponse>> FetchGarden(string profileId);
}