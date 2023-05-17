using EliteAPI.Services.HypixelService;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class ProfileController : ControllerBase
{
    private readonly IHypixelService _hypixelService;
    [GeneratedRegex("[a-zA-Z0-9]{32}")] private static partial Regex IsAlphaNumeric();
    public ProfileController(IHypixelService hypixelService)
    {
        _hypixelService = hypixelService;
    }

    // GET api/<ProfileController>/5
    [HttpGet("{uuid}")]
    public async Task<ActionResult> Get(string uuid)
    {
        if (uuid == null || uuid.Length != 32)
        {
            return BadRequest("UUID must be 32 characters in length and match [a-Z0-9].");
        }

        return await _hypixelService.FetchProfiles(uuid);
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
