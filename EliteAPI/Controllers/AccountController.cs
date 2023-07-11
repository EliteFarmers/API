using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.MojangService;
using EliteAPI.Services.ProfileService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IProfileService _profileService;
    private readonly IAccountService _accountService;
    private readonly IMojangService _mojangService;
    private readonly IMapper _mapper;

    public AccountController(DataContext context, IProfileService profileService, IAccountService accountService, IMojangService mojangService, IMapper mapper)
    {
        _context = context;
        _profileService = profileService;
        _accountService = accountService;
        _mojangService = mojangService;
        _mapper = mapper;
    }

    // GET api/<ValuesController>
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult<AccountEntities>> Get()
    {
        if (HttpContext.Items["Account"] is not AccountEntities result)
        {
            return Unauthorized("Account not found.");
        }

        var account = await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(result.Id));

        return Ok(_mapper.Map<AuthorizedAccountDto>(account));
    }
    
    // GET api/<ValuesController>/12793764936498429
    [HttpGet("{discordId:long}")]
    public async Task<ActionResult<MinecraftAccountDto>> GetByDiscordId(long discordId)
    {
        if (discordId <= 0) return BadRequest("Invalid Discord ID.");

        var account = await _accountService.GetAccount((ulong) discordId);
        if (account is null) return NotFound("Account not found.");
        
        var minecraftAccount = account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();
        if (minecraftAccount is null) return NotFound("User doesn't have any linked Minecraft accounts.");
        
        var profiles = await _profileService.GetPlayersProfiles(minecraftAccount.Id);
        if (profiles.Count == 0) return NotFound("No profiles matching this UUID were found");

        var mappedProfiles = _mapper.Map<List<ProfileDetailsDto>>(profiles);

        var selected = await _profileService.GetSelectedProfileUuid(minecraftAccount.Id);
        if (selected is not null) mappedProfiles.ForEach(p => p.Selected = p.ProfileId == selected);

        var playerData = await _profileService.GetPlayerData(minecraftAccount.Id);
        
        var mappedPlayerData = _mapper.Map<PlayerDataDto>(playerData);
        var result = _mapper.Map<MinecraftAccountDto>(minecraftAccount);

        result.DiscordId = account.Id;
        result.DiscordUsername = account.Username;
        result.PlayerData = mappedPlayerData;
        result.Profiles = mappedProfiles;
        
        return Ok(result);
    }
    
    // GET api/<ValuesController>/12793764936498429
    [HttpGet("{playerUuidOrIgn}")]
    public async Task<ActionResult<MinecraftAccountDto>> GetByPlayerUuidOrIgn(string playerUuidOrIgn) {
        var account = await _accountService.GetAccountByIgnOrUuid(playerUuidOrIgn);
        
        var minecraftAccount = account is null
            ? await _mojangService.GetMinecraftAccountByUuidOrIgn(playerUuidOrIgn)
            : account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();

        if (minecraftAccount is null) {
            return NotFound("Minecraft account not found.");
        }
        
        var profiles = await _profileService.GetPlayersProfiles(minecraftAccount.Id);
        if (profiles.Count == 0) return NotFound("No profiles matching this UUID were found");

        var mappedProfiles = _mapper.Map<List<ProfileDetailsDto>>(profiles);

        var selected = await _profileService.GetSelectedProfileUuid(minecraftAccount.Id);
        if (selected is not null) mappedProfiles.ForEach(p => p.Selected = p.ProfileId == selected);

        var playerData = await _profileService.GetPlayerData(minecraftAccount.Id);
        
        var mappedPlayerData = _mapper.Map<PlayerDataDto>(playerData);
        var result = _mapper.Map<MinecraftAccountDto>(minecraftAccount);

        result.DiscordId = account?.Id;
        result.DiscordUsername = account?.Username;
        result.PlayerData = mappedPlayerData;
        result.Profiles = mappedProfiles;
        
        return Ok(result);
    }

    [HttpPost("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult<AccountEntities>> LinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not AccountEntities loggedInAccount)
        {
            return Unauthorized("Account not found.");
        }

        var account = await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(loggedInAccount.Id));

        if (account is null)
        {
            return Unauthorized("Account not found.");
        }

        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");

        // Check if the player has already linked their account
        if (account.MinecraftAccounts.Any(mc => mc.Id.Equals(id) || mc.Name.Equals(id)))
        {
            return BadRequest("Player has already linked this account.");
        }

        var playerData = await _profileService.GetPlayerDataByUuidOrIgn(id, true);

        if (playerData?.MinecraftAccount is null)
        {
            return BadRequest("No Minecraft account found for this player.");
        }

        var linkedDiscord = playerData.SocialMedia?.Discord;
        if (linkedDiscord is null)
        {
            return BadRequest("Player has not linked their discord.");
        }

        // Handle old Discord accounts with the discriminator (rip) 
        if (account.Discriminator is not null && !account.Discriminator.Equals("0"))
        {
            if (!linkedDiscord.Equals($"{account.Username}#{account.Discriminator}"))
            {
                return BadRequest("Player has a different account linked.");
            }
        } 
        else if (!account.Username.Equals(linkedDiscord)) // Handle new Discord accounts without the discriminator
        {
            return BadRequest("Player has a different account linked.");
        }

        // Success
        account.MinecraftAccounts.Add(playerData.MinecraftAccount);
        
        // Select the account if it's the only one
        if (account.MinecraftAccounts.Count == 1)
        {
            playerData.MinecraftAccount.Selected = true;
        }
        
        // Set the account id
        playerData.MinecraftAccount.AccountId = account.Id;
        _context.MinecraftAccounts.Update(playerData.MinecraftAccount);
        
        await _context.SaveChangesAsync();
        
        return Accepted();
    }

    [HttpDelete("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult<AccountEntities>> UnlinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not AccountEntities linkedAccount)
        {
            return Unauthorized("Account not found.");
        }

        var account = await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(linkedAccount.Id));

        if (account is null) return Unauthorized("Account not found.");
        
        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");
        var minecraftAccount = account.MinecraftAccounts.FirstOrDefault(mc => mc.Id.Equals(id) || mc.Name.Equals(id));

        // Check if the player has already linked their account
        if (minecraftAccount is null)
        {
            return BadRequest("Player has not linked this account.");
        }
        
        // Reset the account id
        minecraftAccount.AccountId = null;
        minecraftAccount.Selected = false;
        account.MinecraftAccounts.Remove(minecraftAccount);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
