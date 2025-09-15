﻿using EliteFarmers.HypixelAPI.DTOs;
using Refit;

namespace EliteFarmers.HypixelAPI;

[Headers("Content-Type: application/json")]
public interface IHypixelApi {
	public const string BaseHypixelUrl = "https://api.hypixel.net/v2";
	
	/// <summary>
	/// Fetches the SkyBlock profiles associated with the given UUID.
	/// <a href="https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1skyblock~1profiles/get">Hypixel API Documentation</a>
	/// 
	/// This endpoint requires an API key.
	/// </summary>
	/// <param name="uuid"></param>
	/// <param name="ct">Cancellation Token</param>
	[Get("/skyblock/profiles?uuid={uuid}")]
	Task<ApiResponse<ProfilesResponse>> FetchProfilesAsync(string uuid, CancellationToken ct = default);
	
	
	/// <summary>
	/// Fetches the player data for the given UUID.
	/// <a href="https://api.hypixel.net/#tag/Player-Data/paths/~1v2~1player/get">Hypixel API Documentation</a>
	/// 
	/// This endpoint requires an API key.
	/// </summary>
	/// <param name="uuid"></param>
	/// <param name="ct">Cancellation Token</param>
	[Get("/player?uuid={uuid}")]
	Task<ApiResponse<PlayerResponse>> FetchPlayerAsync(string uuid, CancellationToken ct = default);
	
	/// <summary>
	/// Fetches the garden data for the given SkyBlock profile ID.
	/// <a href="https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1skyblock~1garden/get">Hypixel API Documentation</a>
	/// 
	/// This endpoint requires an API key.
	/// </summary>
	/// <param name="profileId"></param>
	/// <param name="ct">Cancellation Token</param>
	[Get("/skyblock/garden?profile={profileId}")]
	Task<ApiResponse<GardenResponse>> FetchGardenAsync(string profileId, CancellationToken ct = default);
	
	/// <summary>
	/// Fetches the current Bazaar data.
	/// <a href="https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1skyblock~1bazaar/get">Hypixel API Documentation</a>
	/// </summary>
	/// <param name="ct">Cancellation Token</param>
	[Get("/skyblock/bazaar")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<BazaarResponse>> FetchBazaarAsync(CancellationToken ct = default);
	
	/// <summary>
	/// Fetches the current SkyBlock items data.
	/// <a href="https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1resources~1skyblock~1items/get">Hypixel API Documentation</a>
	/// </summary>
	/// <param name="ct">Cancellation Token</param>
	[Get("/resources/skyblock/items")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<ItemsResponse>> FetchItemsAsync(CancellationToken ct = default);
	
	/// <summary>
	/// Fetches the Auction House data for the specified page.
	/// <a href="https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1skyblock~1auctions/get">Hypixel API Documentation</a>
	/// </summary>
	/// <param name="page">The page number to fetch (starting from 0).</param>
	/// <param name="ct">Cancellation Token</param>
	[Get("/skyblock/auctions?page={page}")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<AuctionHouseResponse>> FetchAuctionHouseAsync(int page, CancellationToken ct = default);
	
	/// <summary>
	/// Fetches the current or upcoming Firesales data.
	/// <a href="https://api.hypixel.net/#tag/SkyBlock/paths/~1v2~1skyblock~1firesales/get">Hypixel API Documentation</a>
	/// </summary>
	/// <param name="ct">Cancellation Token</param>
	[Get("/skyblock/firesales")]
	[Headers("API-Key:")] // Clears API-Key, as it's not required for this endpoint
	Task<ApiResponse<FiresalesResponse>> FetchFiresalesAsync(CancellationToken ct = default);
}