using System.Net;
using EliteAPI.Services.Interfaces;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Refit;
using ErrorOr;

namespace EliteAPI.Services;

public class HypixelService(
	IHypixelApi hypixelApi,
	ILogger<HypixelService> logger)
	: IHypixelService
{
	public static readonly string HttpClientName = "EliteDev";
	
	public async Task<ActionResult<ProfilesResponse>> FetchProfiles(string uuid, CancellationToken cancellationToken = default) {
		if (uuid.Length is not (32 or 36)) return new BadRequestResult();
		var response = await hypixelApi.FetchProfilesAsync(uuid, cancellationToken);

		if (!response.IsSuccessStatusCode) {
			LogRateLimitWarnings(response);
			logger.LogError("Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, response.StatusCode);
			return new BadRequestResult();
		}

		if (response.Content is not { Success: true }) return new BadRequestResult();

		return response.Content;
	}

	public async Task<ActionResult<PlayerResponse>> FetchPlayer(string uuid, CancellationToken cancellationToken = default) {
		if (uuid.Length is not (32 or 36)) return new BadRequestResult();
		var response = await hypixelApi.FetchPlayerAsync(uuid, cancellationToken);

		if (!response.IsSuccessStatusCode) {
			LogRateLimitWarnings(response);
			logger.LogError("Failed to fetch player for {Uuid}, Error: {Error}", uuid, response.StatusCode);
			return new BadRequestResult();
		}

		if (response.Content is not { Success: true }) return new BadRequestResult();

		return response.Content;
	}

	public async Task<ActionResult<GardenResponse>> FetchGarden(string profileId, CancellationToken cancellationToken = default) {
		if (profileId.Length is not (32 or 36)) return new BadRequestResult();
		var response = await hypixelApi.FetchGardenAsync(profileId, cancellationToken);

		if (!response.IsSuccessStatusCode) {
			if (response.StatusCode == HttpStatusCode.NotFound) return new NotFoundResult();
			LogRateLimitWarnings(response);

			logger.LogError("Failed to fetch garden for {Uuid}, Error: {Error}", profileId, response.StatusCode);

			return new BadRequestResult();
		}

		if (response.Content is not { Success: true }) return new BadRequestResult();

		return response.Content;
	}

	public async Task<ErrorOr<MuseumResponse>> FetchMuseum(string profileId, CancellationToken cancellationToken = default) {
		if (profileId.Length is not (32 or 36)) return Error.Validation("ProfileId", "Invalid ProfileId length");
		var response = await hypixelApi.FetchMuseumAsync(profileId, cancellationToken);

		if (!response.IsSuccessStatusCode) {
			if (response.StatusCode == HttpStatusCode.NotFound) return Error.NotFound("Museum", "Museum not found");
			LogRateLimitWarnings(response);

			logger.LogError("Failed to fetch museum for {ProfileId}, Error: {Error}", profileId, response.StatusCode);

			return Error.Failure("HypixelAPI", "Failed to fetch museum data");
		}

		if (response.Content is not { Success: true }) return Error.Failure("HypixelAPI", "Failed to fetch museum data");

		return response.Content;
	}

	private void LogRateLimitWarnings<T>(ApiResponse<T> response) {
		if (response.StatusCode != HttpStatusCode.TooManyRequests) return;

		response.Headers.TryGetValues("ratelimit-limit", out var limit);

		if (limit is not null)
			logger.LogWarning("Hypixel API rate limit exceeded! Limit: {Limit}", limit);
		else logger.LogWarning("Hypixel API rate limit exceeded!");
	}
}