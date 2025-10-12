using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IHypixelService {
	public Task<ActionResult<ProfilesResponse>> FetchProfiles(string uuid);
	public Task<ActionResult<PlayerResponse>> FetchPlayer(string uuid);
	public Task<ActionResult<GardenResponse>> FetchGarden(string profileId);
}