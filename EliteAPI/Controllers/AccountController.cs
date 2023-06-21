using Discord;
using Discord.Rest;
using EliteAPI.Authentication;
using EliteAPI.Models.Entities;
using EliteAPI.Services.AccountService;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{

    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // GET api/<ValuesController>/5
    [HttpGet("{id}")]
    //[ApiKeyAuthFilter]
    public async Task<ActionResult<Account>> Get(int id)
    {
        var result = await _accountService.GetAccount(id);
        if (result is null)
        {
            return NotFound("Account not found.");
        }
        return Ok(result);
    }

    // POST api/<ValuesController>
    /// <summary>
    /// Creates a new account provided a Discord access token.
    /// </summary>
    /// <param name="token"></param>
    [HttpPost]
    public async Task<ActionResult> Post([FromBody] string token)
    {

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Discord access token is required.");
        }

        var discordRestClient = new DiscordRestClient();
        await discordRestClient.LoginAsync(TokenType.Bearer, token);
        var discordUser = await discordRestClient.GetCurrentUserAsync();

        if (discordUser is null)
        {
            return BadRequest("Invalid Discord access token.");
        }

        var user = new Account()
        {
            DisplayName = discordUser.Username,
            Id = discordUser.Id,
            Username = discordUser.Username,
            Discriminator = discordUser.Discriminator,
            Email = discordUser.Email,
        };

        await _accountService.AddAccount(user);

        return Ok();
    }

    // PUT api/<ValuesController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<ValuesController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
