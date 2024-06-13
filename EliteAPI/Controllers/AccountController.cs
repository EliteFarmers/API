using System.Text.RegularExpressions;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public partial class AccountController(
    DataContext context, 
    IProfileService profileService, 
    IMemberService memberService,
    IAccountService accountService, 
    IMojangService mojangService, 
    IMapper mapper,
    UserManager<ApiUser> userManager)
    : ControllerBase
{
    
    /// <summary>
    /// Get logged in Minecraft account
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult<AuthorizedAccountDto>> Get()
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
            return BadRequest("Linked account not found.");
        }
        
        var account = await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .ThenInclude(a => a.Badges)
            .FirstOrDefaultAsync(a => a.Id.Equals(user.AccountId));

        return Ok(mapper.Map<AuthorizedAccountDto>(account));
    }
    
    // GET <ValuesController>
    /// <summary>
    /// Search for Minecraft IGNs
    /// </summary>
    /// <param name="query">Query string</param>
    /// <param name="start">Start of results for pagination</param>
    /// <returns></returns>
    [HttpGet("Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<List<string>>> Search([FromQuery(Name = "q")] string query, [FromQuery] string? start = null)
    {
        // Check that the query is a valid username with regex
        if (!UserNameRegex().IsMatch(query))
        {
            return BadRequest("Invalid query.");
        }
        
        // Check that the start param is a valid username with regex
        if (start is not null && !UserNameRegex().IsMatch(start))
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
    /// <summary>
    /// Get Minecraft account by Discord ID
    /// </summary>
    /// <param name="discordId"></param>
    /// <returns></returns>
    [HttpGet("{discordId:long:minlength(17)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<MinecraftAccountDto>> GetByDiscordId(long discordId)
    {
        if (discordId <= 0) return BadRequest("Invalid Discord ID.");

        var account = await accountService.GetAccount((ulong) discordId);
        if (account is null) {
            return await GetByPlayerUuidOrIgn(discordId.ToString());
        }
        
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
        
        return Ok(result);
    }
    
    // GET <ValuesController>/12793764936498429
    /// <summary>
    /// Get Minecraft account by IGN or UUID
    /// </summary>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Link a Minecraft account to a Discord account
    /// </summary>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> LinkAccount(string playerUuidOrIgn)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
            return Unauthorized("Account not found.");
        }
        
        return await accountService.LinkAccount(user.AccountId.Value, playerUuidOrIgn);
    }

    /// <summary>
    /// Unlink a Minecraft account from a Discord account
    /// </summary>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    [HttpDelete("{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> UnlinkAccount(string playerUuidOrIgn)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
            return Unauthorized("Account not found.");
        }

        return await accountService.UnlinkAccount(user.AccountId.Value, playerUuidOrIgn);
    }
    
    /// <summary>
    /// Mark a Minecraft account as primary
    /// </summary>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("primary/{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> MakePrimaryAccount(string playerUuidOrIgn)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
            return Unauthorized("Account not found.");
        }

        return await accountService.MakePrimaryAccount(user.AccountId.Value, playerUuidOrIgn);
    }

    [GeneratedRegex("^[a-zA-Z0-9_]{1,26}$")]
    private static partial Regex UserNameRegex();
}
