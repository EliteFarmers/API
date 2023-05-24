using EliteAPI.Data.Models.Hypixel;
using EliteAPI.Services.HypixelService;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using EliteAPI.Services.ProfileService;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    [GeneratedRegex("[a-zA-Z0-9]{32}")] private static partial Regex IsAlphaNumeric();
    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    // GET api/<ProfileController>/5
    [HttpGet("{uuid}/selected")]
    public async Task<ActionResult<ProfileMember>> Get(string uuid)
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

        return Ok(member);
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
