using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers.Bot; 

[Route("[controller]")]
[ServiceFilter(typeof(DiscordBotOnlyFilter))]
[ApiController]
public class BotController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IDiscordService _discordService;
    private readonly IAccountService _accountService;
    private readonly ILogger<BotController> _logger;

    public BotController(DataContext context, IMapper mapper, IDiscordService discordService, IAccountService accountService, ILogger<BotController> logger)
    {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
        _accountService = accountService;
        _logger = logger;
    }
    
    // GET <BotController>/12793764936498429
    [HttpGet("{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<GuildDto>> Get(ulong guildId)
    {
        await _discordService.RefreshBotGuilds();
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");
        
        return Ok(_mapper.Map<GuildDto>(guild));
    }
    
    // GET <BotController>/12793764936498429/jacob
    [HttpGet("{guildId}/jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> GetJacobFeature(ulong guildId)
    {
        await _discordService.RefreshBotGuilds();
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");
        
        if (!guild.Features.JacobLeaderboardEnabled) {
            return BadRequest("Jacob leaderboards are not enabled for this guild");
        }

        if (guild.Features.JacobLeaderboard is not null) return Ok(guild.Features.JacobLeaderboard);
        
        guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
        
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();

        return Ok(guild.Features.JacobLeaderboard);
    }
    
    // PUT <GuildController>/12793764936498429/jacob
    [HttpPut("{guildId}/jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> PutJacobFeature(ulong guildId, [FromBody] GuildJacobLeaderboardFeature data)
    {
        await _discordService.RefreshBotGuilds();
        
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");

        if (!guild.Features.JacobLeaderboardEnabled) {
            return BadRequest("Jacob leaderboards are not enabled for this guild");
        }

        guild.Features.JacobLeaderboard = data;
        
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();
        
        return Accepted();
    }
    
    // Patch <GuildController>/account
    [HttpPatch("account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthorizedAccountDto>> PatchAccount([FromBody] IncomingAccountDto incoming) {
        var exising = await _accountService.GetAccount(incoming.Id);
        
        var account = exising ?? new EliteAccount {
            Id = incoming.Id,
            Username = incoming.Username,
            DisplayName = incoming.DisplayName ?? incoming.Username,
        };

        account.Avatar = incoming.Avatar ?? account.Avatar;
        account.DisplayName = incoming.DisplayName ?? account.DisplayName;
        account.Locale = incoming.Locale ?? account.Locale;
        
        account.Discriminator = incoming.Discriminator;

        if (exising is null) {
            try {
                await _context.Accounts.AddAsync(account);
                await _context.SaveChangesAsync();
            } catch (Exception e) {
                _logger.LogWarning("Failed to add account to database: {Error}", e);
            }
        } else {
            _context.Accounts.Update(account);
        }
        
        return Ok(_mapper.Map<AuthorizedAccountDto>(account));
    }
    
    // Post <GuildController>/account/12793764936498429/Ke5o
    [HttpPost("account/{discordId:long:min(0)}/{playerIgnOrUuid}")]
    public async Task<ActionResult> LinkAccount(long discordId, string playerIgnOrUuid) {
        return await _accountService.LinkAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    // Delete <GuildController>/account/12793764936498429/Ke5o
    [HttpDelete("account/{discordId:long:min(0)}/{playerIgnOrUuid}")]
    public async Task<ActionResult> UnlinkAccount(long discordId, string playerIgnOrUuid) {
        return await _accountService.UnlinkAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    // Post <GuildController>/account/12793764936498429/Ke5o/primary
    [HttpPost("account/{discordId:long:min(0)}/{playerIgnOrUuid}/primary")]
    public async Task<ActionResult> MakePrimaryAccount(long discordId, string playerIgnOrUuid) {
        return await _accountService.MakePrimaryAccount((ulong) discordId, playerIgnOrUuid);
    }
}