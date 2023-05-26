using EliteAPI.Models.DTOs.Incoming;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.HypixelService;

public interface IHypixelService
{
    public Task<ActionResult<RawProfilesResponse>> FetchProfiles(string uuid);
    public Task<ActionResult> FetchPlayer(string uuid);
}
