using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using ErrorOr;

namespace EliteAPI.Services.Interfaces;

public interface IHypixelService
{
	public Task<ActionResult<ProfilesResponse>> FetchProfiles(string uuid, CancellationToken cancellationToken = default);
	public Task<ActionResult<PlayerResponse>> FetchPlayer(string uuid, CancellationToken cancellationToken = default);
	public Task<ActionResult<GardenResponse>> FetchGarden(string profileId, CancellationToken cancellationToken = default);
	public Task<ErrorOr<MuseumResponse>> FetchMuseum(string profileId, CancellationToken cancellationToken = default);
}