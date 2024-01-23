using System.Text.RegularExpressions;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.MemberService;
using EliteAPI.Services.MojangService;
using EliteAPI.Services.ProfileService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class AccountController(DataContext context, IProfileService profileService, IMemberService memberService,
        IAccountService accountService, IMojangService mojangService, IMapper mapper)
    : ControllerBase
{
    // GET <ValuesController>
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult<AuthorizedAccountDto>> Get()
    {
        if (HttpContext.Items["Account"] is not EliteAccount result)
        {
            return Unauthorized("Account not found.");
        }

        var account = await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(result.Id));

        return Ok(mapper.Map<AuthorizedAccountDto>(account));
    }
    
    // GET <ValuesController>
    [HttpGet("Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<List<string>>> Search([FromQuery(Name = "q")] string query, [FromQuery] string? start = null)
    {
        // Check that the query is a valid username or UUID with regex
        if (!Regex.IsMatch(query, "^[a-zA-Z0-9_]{1,26}$"))
        {
            return BadRequest("Invalid query.");
        }
        
        // Check that the start param is a valid username or UUID with regex
        if (start is not null && !Regex.IsMatch(start, "^[a-zA-Z0-9_]{1,26}$"))
        {
            return BadRequest("Invalid query.");
        }
        
        // Make dbParameters
        var dbQuery = new Npgsql.NpgsqlParameter("query", query);
        var dbStart = new Npgsql.NpgsqlParameter("start", start ?? query);
        var dbEnd = new Npgsql.NpgsqlParameter("end", query + "ÿ");
        
        // Execute autocomplete_igns stored procedure
        var result = await context.Database
            .SqlQuery<string>($"SELECT * FROM autocomplete_igns({dbQuery}, {dbStart}, {dbEnd})")
            .ToListAsync();
        
        return Ok(result);
    }
    
    // GET <ValuesController>/12793764936498429
    [HttpGet("{discordId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<MinecraftAccountDto>> GetByDiscordId(long discordId)
    {
        if (discordId <= 0) return BadRequest("Invalid Discord ID.");

        var account = await accountService.GetAccount((ulong) discordId);
        if (account is null) return NotFound("Account not found.");
        
        var minecraftAccount = account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();
        if (minecraftAccount is null) return NotFound("User doesn't have any linked Minecraft accounts.");

        var profileDetails = await profileService.GetProfilesDetails(minecraftAccount.Id);
        
        var playerData = await profileService.GetPlayerData(minecraftAccount.Id);
        var result = mapper.Map<MinecraftAccountDto>(minecraftAccount);

        result.DiscordId = account.Id.ToString();
        result.DiscordUsername = account.Username;
        result.DiscordAvatar = account.Avatar;
        result.PlayerData = mapper.Map<PlayerDataDto>(playerData);
        result.Profiles = profileDetails;
        result.EventEntries = mapper.Map<List<EventMemberDetailsDto>>(account.EventEntries);
        
        return Ok(result);
    }
    
    // GET <ValuesController>/12793764936498429
    [HttpGet("{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<MinecraftAccountDto>> GetByPlayerUuidOrIgn(string playerUuidOrIgn) {
        await memberService.UpdatePlayerIfNeeded(playerUuidOrIgn);
        
        var account = await accountService.GetAccountByIgnOrUuid(playerUuidOrIgn);
        
        var minecraftAccount = account is null
            ? await mojangService.GetMinecraftAccountByUuidOrIgn(playerUuidOrIgn)
            : account.MinecraftAccounts.Find(m => m.Id.Equals(playerUuidOrIgn) || m.Name.Equals(playerUuidOrIgn)) 
              ?? account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();

        if (minecraftAccount is null) {
            return NotFound("Minecraft account not found.");
        }
        
        var profilesDetails = await profileService.GetProfilesDetails(minecraftAccount.Id);
        var playerData = await profileService.GetPlayerData(minecraftAccount.Id);
        
        var mappedPlayerData = mapper.Map<PlayerDataDto>(playerData);
        var result = mapper.Map<MinecraftAccountDto>(minecraftAccount);

        result.DiscordId = account?.Id.ToString();
        result.DiscordUsername = account?.Username;
        result.DiscordAvatar = account?.Avatar;
        result.PlayerData = mappedPlayerData;
        result.Profiles = profilesDetails;
        
        return Ok(result);
    }

    [HttpPost("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> LinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not EliteAccount loggedInAccount)
        {
            return Unauthorized("Account not found.");
        }

        return await accountService.LinkAccount(loggedInAccount.Id, playerUuidOrIgn);
    }

    [HttpDelete("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> UnlinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not EliteAccount linkedAccount)
        {
            return Unauthorized("Account not found.");
        }

        return await accountService.UnlinkAccount(linkedAccount.Id, playerUuidOrIgn);
    }
    
    [HttpPost("primary/{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> MakePrimaryAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not EliteAccount loggedInAccount)
        {
            return Unauthorized("Account not found.");
        }

        return await accountService.MakePrimaryAccount(loggedInAccount.Id, playerUuidOrIgn);
    }
}
