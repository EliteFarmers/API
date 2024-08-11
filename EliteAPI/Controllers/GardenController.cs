using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
[ApiController, ApiVersion(1.0)]
public class GardenController(
    IProfileService profileService, 
    IMapper mapper)
    : ControllerBase 
{
    /// <summary>
    /// Selected Garden Of Player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    [HttpGet("{playerUuid}/Selected")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<GardenDto>> GetSelectedByPlayerUuid(string playerUuid)
    {
        if (playerUuid is not { Length: 32 })
        {
            return BadRequest("UUID must be 32 characters in length and match [a-Z0-9].");
        }

        var garden = await profileService.GetSelectedGarden(playerUuid);

        if (garden is null)
        {
            return NotFound("No selected profile found for this UUID.");
        }

        var mapped = mapper.Map<GardenDto>(garden);

        return Ok(mapped);
    }
    
    /// <summary>
    /// Get Garden
    /// </summary>
    /// <param name="profileUuid"></param>
    /// <returns></returns>
    [HttpGet("{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<GardenDto>> Get(string profileUuid)
    {
        var garden = await profileService.GetProfileGarden(profileUuid);
        if (garden is null)
        {
            return NotFound("No profile matching this UUID was found");
        }

        var mapped = mapper.Map<GardenDto>(garden);
        return Ok(mapped);
    }
}
