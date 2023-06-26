using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using EliteAPI.Services.ProfileService;
using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IMapper _mapper;
    [GeneratedRegex("[a-zA-Z0-9]{32}")] private static partial Regex IsAlphaNumeric();

    public ProfileController(IProfileService profileService, IMapper mapper)
    {
        _profileService = profileService;
        _mapper = mapper;
    }

    // GET api/<ProfileController>/[uuid]/Selected
    [HttpGet("{uuid}/Selected")]
    public async Task<ActionResult<ProfileMemberDto>> GetSelectedByPlayerUuid(string uuid)
    {
        if (uuid is not { Length: 32 })
        {
            return BadRequest("UUID must be 32 characters in length and match [a-Z0-9].");
        }

        var member = await _profileService.GetSelectedProfileMember(uuid);

        if (member is null)
        {
            return NotFound("No selected profile member found for this UUID.");
        }

        Console.WriteLine(member.JacobData?.Perks?.DoubleDrops);
        
        var mapped = _mapper.Map<ProfileMemberDto>(member);

        return Ok(mapped);
    }

    // POST api/<ProfileController>
    [HttpGet("{profileUuid}")]
    public async Task<ActionResult<ProfileDto>> Get(string profileUuid)
    {
        var profile = await _profileService.GetProfile(profileUuid);
        if (profile is null)
        {
            return NotFound("No profile matching this UUID was found");
        }

        var mapped = _mapper.Map<ProfileDto>(profile);
        return Ok(mapped);
    }

    // POST api/<ProfileController>s
    [Route("/api/[controller]s/{playerUuid}")]
    [HttpGet]
    public async Task<ActionResult<List<ProfileDto>>> GetProfiles(string playerUuid)
    {
        var profiles = await _profileService.GetPlayersProfiles(playerUuid);

        if (profiles.Count == 0)
        {
            return NotFound("No profiles matching this UUID were found");
        }

        return Ok(_mapper.Map<List<ProfileDto>>(profiles));
    }
}
