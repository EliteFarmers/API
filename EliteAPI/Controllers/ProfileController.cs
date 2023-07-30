using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using EliteAPI.Services.ProfileService;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IMapper _mapper;
    private readonly DataContext _context;

    public ProfileController(IProfileService profileService, IMapper mapper, DataContext dataContext)
    {
        _profileService = profileService;
        _mapper = mapper;
        _context = dataContext;
    }

    // GET <ProfileController>/[uuid]/Selected
    [HttpGet("{uuid}/Selected")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
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

        var mapped = _mapper.Map<ProfileMemberDto>(member);

        return Ok(mapped);
    }

    // GET <ProfileController>
    [HttpGet("{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ProfileMemberDto>> GetSpecificProfile(string playerUuid, string profileUuid)
    {
        var profile = await _profileService.GetProfileMember(profileUuid, playerUuid);
        if (profile is null)
        {
            return NotFound("No profile matching this UUID was found for this player");
        }

        var mapped = _mapper.Map<ProfileMemberDto>(profile);
        return Ok(mapped);
    }
    
    // GET <ProfileController>
    [HttpGet("{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ProfileDetailsDto>> Get(string profileUuid)
    {
        var profile = await _profileService.GetProfile(profileUuid);
        if (profile is null)
        {
            return NotFound("No profile matching this UUID was found");
        }

        var mapped = _mapper.Map<ProfileDetailsDto>(profile);
        return Ok(mapped);
    }

    // GET <ProfileController>s
    [Route("/[controller]s/{playerUuid}")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<List<ProfileDetailsDto>>> GetProfiles(string playerUuid)
    {
        var profiles = await _profileService.GetProfilesDetails(playerUuid);

        if (profiles.Count == 0)
        {
            return NotFound("No profiles matching this UUID were found");
        }
        
        return Ok(profiles);
    }
}
