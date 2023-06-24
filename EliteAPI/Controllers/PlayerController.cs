using Microsoft.AspNetCore.Mvc;
using EliteAPI.Services.ProfileService;
using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlayerController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IMapper _mapper;

    public PlayerController(IProfileService profileService, IMapper mapper)
    {
        _profileService = profileService;
        _mapper = mapper;
    }

    // POST api/<ProfileController>
    [HttpGet("{playerUuid}")]
    public async Task<ActionResult<PlayerDataDto>> Get(string playerUuid)
    {
        var playerData = await _profileService.GetPlayerData(playerUuid);
        if (playerData is null)
        {
            return NotFound("No player matching this UUID was found");
        }

        var mapped = _mapper.Map<PlayerDataDto>(playerData);
        return Ok(mapped);
    }
}
