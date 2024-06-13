using EliteAPI.Models.DTOs.Incoming;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IHypixelService
{
    public Task<ActionResult<RawProfilesResponse>> FetchProfiles(string uuid);
    public Task<ActionResult<RawPlayerResponse>> FetchPlayer(string uuid);
}
