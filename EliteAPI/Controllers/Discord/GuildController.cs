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
public class GuildController(
    DataContext context, 
    IMapper mapper, 
    IDiscordService discordService)
    : Controller 
{
    /// <summary>
    /// Get list of public guilds
    /// </summary>
    /// <returns></returns>
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GuildDetailsDto>>> GetGuilds() {
        var guilds = await context.Guilds
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
    
    /// <summary>
    /// Get guild by ID
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <returns></returns>
    [HttpGet("{guildId}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<PublicGuildDto>> GetGuildById(long guildId) {
        if (guildId <= 0) return BadRequest("Invalid guild ID.");

        await discordService.RefreshBotGuilds();
        
        var guild = await context.Guilds.FindAsync((ulong) guildId);
        if (guild is null) return NotFound("Guild not found.");
        
        return Ok(mapper.Map<PublicGuildDto>(guild));
    }
    
    /// <summary>
    /// Get guild events
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <returns></returns>
    [HttpGet("{guildId}/Events")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<List<EventDetailsDto>>> GetGuildEvents(long guildId) {
        if (guildId <= 0) return BadRequest("Invalid guild ID.");

        await discordService.RefreshBotGuilds();
        
        var guild = await context.Guilds.FindAsync((ulong) guildId);
        if (guild is null) return NotFound("Guild not found.");
        
        var events = await context.Events
            .Where(e => e.GuildId == guild.Id)
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .ToListAsync();
        
        return Ok(mapper.Map<List<EventDetailsDto>>(events) ?? []);
    }
}