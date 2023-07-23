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

[Route("[controller]")]
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

    // GET <ValuesController>
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult<AuthorizedAccountDto>> Get()
    {
        if (HttpContext.Items["Account"] is not AccountEntity result)
        {
            return Unauthorized("Account not found.");
        }

        var account = await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(result.Id));

        return Ok(_mapper.Map<AuthorizedAccountDto>(account));
    }
    
    // GET <ValuesController>/12793764936498429
    [HttpGet("{discordId:long}")]
    public async Task<ActionResult<MinecraftAccountDto>> GetByDiscordId(long discordId)
    {
        if (discordId <= 0) return BadRequest("Invalid Discord ID.");

        var account = await _accountService.GetAccount((ulong) discordId);
        if (account is null) return NotFound("Account not found.");
        
        var minecraftAccount = account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();
        if (minecraftAccount is null) return NotFound("User doesn't have any linked Minecraft accounts.");
        
        var selected = await _profileService.GetSelectedProfileUuid(minecraftAccount.Id);
        var profiles = await _profileService.GetPlayersProfiles(minecraftAccount.Id);

        var mappedProfiles = _mapper.Map<List<ProfileDetailsDto>>(profiles);
        if (selected is not null) mappedProfiles.ForEach(p => p.Selected = p.ProfileId == selected);

        var playerData = await _profileService.GetPlayerData(minecraftAccount.Id);
        
        var mappedPlayerData = _mapper.Map<PlayerDataDto>(playerData);
        var result = _mapper.Map<MinecraftAccountDto>(minecraftAccount);

        result.DiscordId = account.Id.ToString();
        result.DiscordUsername = account.Username;
        result.DiscordAvatar = account.Avatar;
        result.PlayerData = mappedPlayerData;
        result.Profiles = mappedProfiles;
        
        return Ok(result);
    }
    
    // GET <ValuesController>/12793764936498429
    [HttpGet("{playerUuidOrIgn}")]
    public async Task<ActionResult<MinecraftAccountDto>> GetByPlayerUuidOrIgn(string playerUuidOrIgn) {
        var account = await _accountService.GetAccountByIgnOrUuid(playerUuidOrIgn);
        
        var minecraftAccount = account is null
            ? await _mojangService.GetMinecraftAccountByUuidOrIgn(playerUuidOrIgn)
            : account.MinecraftAccounts.Find(m => m.Id.Equals(playerUuidOrIgn) || m.Name.Equals(playerUuidOrIgn)) 
              ?? account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();

        if (minecraftAccount is null) {
            return NotFound("Minecraft account not found.");
        }
        
        var profiles = await _profileService.GetPlayersProfiles(minecraftAccount.Id);
        // This needs to be fetched because "selected" lives on the ProfileMembers
        var selected = await _profileService.GetSelectedProfileUuid(minecraftAccount.Id);
        
        var mappedProfiles = _mapper.Map<List<ProfileDetailsDto>>(profiles);

        if (selected is not null) mappedProfiles.ForEach(p => p.Selected = p.ProfileId == selected);

        var playerData = await _profileService.GetPlayerData(minecraftAccount.Id);
        
        var mappedPlayerData = _mapper.Map<PlayerDataDto>(playerData);
        var result = _mapper.Map<MinecraftAccountDto>(minecraftAccount);

        result.DiscordId = account?.Id.ToString();
        result.DiscordUsername = account?.Username;
        result.DiscordAvatar = account?.Avatar;
        result.PlayerData = mappedPlayerData;
        result.Profiles = mappedProfiles;
        
        return Ok(result);
    }

    [HttpPost("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult> LinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not AccountEntity loggedInAccount)
        {
            return Unauthorized("Account not found.");
        }

        return await _accountService.LinkAccount(loggedInAccount.Id, playerUuidOrIgn);
    }

    [HttpDelete("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult> UnlinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not AccountEntity linkedAccount)
        {
            return Unauthorized("Account not found.");
        }

        return await _accountService.UnlinkAccount(linkedAccount.Id, playerUuidOrIgn);
    }
    
    [HttpPost("primary/{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult> MakePrimaryAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not AccountEntity loggedInAccount)
        {
            return Unauthorized("Account not found.");
        }

        return await _accountService.MakePrimaryAccount(loggedInAccount.Id, playerUuidOrIgn);
    }
}
