using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers.Discord; 

[Route("[controller]")]
[ApiController]
[ServiceFilter(typeof(DiscordAuthFilter))]
public class UserController : ControllerBase {
    
    private readonly IDiscordService _discordService;
    private readonly DataContext _context;
    private readonly IMapper _mapper;

    public UserController(DataContext context, IMapper mapper, IDiscordService discordService) {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
    }
    
    // GET <GuildController>/Guilds
    [HttpGet("Guilds")]
    public async Task<ActionResult<IEnumerable<UserGuildDto>>> Get() {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        return await _discordService.GetUsersGuilds(account.Id, token);
    }
    
    // GET <GuildController>/Guild/{guildId}
    [HttpGet("Guild/{guildId}")]
    public async Task<ActionResult<AuthorizedGuildDto>> GetGuild(ulong guildId) {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        return Ok(new AuthorizedGuildDto {
            Id = guildId.ToString(),
            Permissions = userGuild.Permissions,
            DiscordGuild = fullGuild,
            Guild = _mapper.Map<GuildDto>(guild)
        });
    }
    
}