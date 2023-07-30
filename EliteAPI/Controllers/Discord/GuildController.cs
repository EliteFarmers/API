using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Discord; 

[Route("[controller]")]
[ApiController]
public class GuildController : Controller {
    private readonly IDiscordService _discordService;
    private readonly DataContext _context;
    private readonly IMapper _mapper;

    public GuildController(DataContext context, IMapper mapper, IDiscordService discordService) {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
    }
    
    // GET <GuildController>s
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<List<GuildDetailsDto>>> GetGuilds() {
        var guilds = await _context.Guilds
            .Where(g => g.InviteCode != null && g.Features.JacobLeaderboardEnabled)
            .OrderByDescending(g => g.MemberCount)
            .Select(g => new GuildDetailsDto {
                Id = g.Id.ToString(), 
                Name = g.Name, 
                InviteCode = g.InviteCode, 
                Banner = g.Banner, 
                Icon = g.Icon,
                MemberCount = g.MemberCount
            })
            .ToListAsync();

        return Ok(guilds);
    }
    
    // GET <GuildController>/[guildId]
    [HttpGet("{guildId:long}")]
    public async Task<ActionResult<PublicGuildDto>> GetGuildById(long guildId) {
        if (guildId <= 0) return BadRequest("Invalid guild ID.");

        await _discordService.RefreshBotGuilds();
        
        var guild = await _context.Guilds.FindAsync((ulong) guildId);
        if (guild is null) return NotFound("Guild not found.");
        
        return Ok(_mapper.Map<PublicGuildDto>(guild));
    }
}