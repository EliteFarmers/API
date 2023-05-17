using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.HypixelService;

public interface IHypixelService
{
    public Task<ActionResult> FetchProfiles(string uuid);
}
