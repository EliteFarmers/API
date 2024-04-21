using Microsoft.AspNetCore.Mvc;
using EliteAPI.Services.ProfileService;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class ProfileController(IProfileService profileService, IMapper mapper, DataContext context)
    : ControllerBase {
    
    /// <summary>
    /// Selected Profile Member
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    // GET <ProfileController>/[uuid]/Selected
    [HttpGet("{uuid}/Selected")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<ProfileMemberDto>> GetSelectedByPlayerUuid(string uuid)
    {
        if (uuid is not { Length: 32 })
        {
            return BadRequest("UUID must be 32 characters in length and match [a-Z0-9].");
        }

        var member = await profileService.GetSelectedProfileMember(uuid);

        if (member is null)
        {
            return NotFound("No selected profile member found for this UUID.");
        }

        var mapped = mapper.Map<ProfileMemberDto>(member);

        return Ok(mapped);
    }

    /// <summary>
    /// Profile Member
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="profileUuid"></param>
    /// <returns></returns>
    // GET <ProfileController>
    [HttpGet("{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<ProfileMemberDto>> GetSpecificProfile(string playerUuid, string profileUuid)
    {
        var profile = await profileService.GetProfileMember(playerUuid, profileUuid);
        if (profile is null)
        {
            return NotFound("No profile matching this UUID was found for this player");
        }

        var mapped = mapper.Map<ProfileMemberDto>(profile);
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get Profile Details
    /// </summary>
    /// <param name="profileUuid"></param>
    /// <returns></returns>
    // GET <ProfileController>
    [HttpGet("{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<ProfileDetailsDto>> Get(string profileUuid)
    {
        var profile = await profileService.GetProfile(profileUuid);
        if (profile is null)
        {
            return NotFound("No profile matching this UUID was found");
        }

        var mapped = mapper.Map<ProfileDetailsDto>(profile);
        return Ok(mapped);
    }

    /// <summary>
    /// Get List of Profile Details
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    // GET <ProfileController>s
    [Route("/[controller]s/{playerUuid}")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<ProfileDetailsDto>>> GetProfiles(string playerUuid)
    {
        var profiles = await profileService.GetProfilesDetails(playerUuid);

        if (profiles.Count == 0)
        {
            return NotFound("No profiles matching this UUID were found");
        }
        
        return Ok(profiles);
    }
    
    /// <summary>
    /// Get List of Profile Names
    /// </summary>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    // GET <ProfileController>s
    [Route("/[controller]s/{playerUuidOrIgn}/Names")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProfileNamesDto>>> GetProfilesNames(string playerUuidOrIgn) {
        var uuid = playerUuidOrIgn;
        
        if (uuid is null or { Length: < 32 }) {
            var player = await profileService.GetPlayerDataByUuidOrIgn(playerUuidOrIgn);
            uuid = player?.Uuid;
            
            if (uuid is null) return Ok(new List<ProfileNamesDto>());
        }
        
        var profiles = await context.ProfileMembers
            .AsNoTracking()
            .Where(m => m.PlayerUuid.Equals(uuid))
            .Select(m => new ProfileNamesDto {
                Id = m.ProfileId,
                Name = m.ProfileName ?? m.Profile.ProfileName,
                Selected = m.IsSelected
            }).ToListAsync();
        
        return Ok(profiles);
    }
}
