using EliteAPI.Services.HypixelService;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using EliteAPI.Services.ProfileService;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Hypixel;

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
    public async Task<ActionResult<ProfileMemberDto>> Get(string uuid)
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
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<ProfileController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<ProfileController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
