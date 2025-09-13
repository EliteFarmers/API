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
	
	[Get("/skyblock/bazaar")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<BazaarResponse>> FetchBazaar();
	
	[Get("/resources/skyblock/items")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<ItemsResponse>> FetchItems();
	
	[Get("/skyblock/auctions?page={page}")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<AuctionHouseResponse>> FetchAuctionHouse(int page);
	
	[Get("/skyblock/firesales")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<FiresalesResponse>> FetchFiresales();
}