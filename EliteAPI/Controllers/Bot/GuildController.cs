using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Bot; 

[Route("[controller]")]
[ServiceFilter(typeof(DiscordBotOnlyFilter))]
[ApiController]
public class GuildController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IDiscordService _discordService;

    public GuildController(DataContext context, IMapper mapper, IDiscordService discordService)
    {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
    }
    
    // GET <GuildController>/12793764936498429
    [HttpGet("{guildId}")]
    public async Task<ActionResult<EventDetailsDto>> Get(ulong guildId)
    {
        await _discordService.RefreshBotGuilds();
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");
        
        return Ok(guild);
    }
    
    // GET <GuildController>/12793764936498429/jacob/12793764936498429
    [HttpPost("{guildId}/jacob/{channelId}/scores")]
    public async Task<ActionResult<EventDetailsDto>> AddJacobRecord(ulong guildId, ulong channelId, [FromBody] GuildJacobLeaderboardEntry entry)
    {
        await _discordService.RefreshBotGuilds();
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");

        if (!guild.Features.JacobLeaderboardEnabled) {
            return BadRequest("Jacob leaderboards are not enabled for this guild");
        }

        var jacob = guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
        
        if (jacob.Leaderboards.Count < 1) {
            return BadRequest("No leaderboards have been created for this guild");
        }
        
        var leaderboard = jacob.Leaderboards.FirstOrDefault(x => x.ChannelId == channelId.ToString());
        if (leaderboard is null) return NotFound("Leaderboard not found");

        var list = new List<GuildJacobLeaderboardEntry>();
        switch (entry.Record.Crop) {
            case "Cactus": 
                leaderboard.Cactus.Add(entry);
                leaderboard.Cactus = leaderboard.Cactus.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Carrot": 
                leaderboard.Carrot.Add(entry);
                leaderboard.Carrot = leaderboard.Carrot.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Cocoa Beans":
                leaderboard.Cocoa.Add(entry);
                leaderboard.Cocoa = leaderboard.Cocoa.OrderByDescending(e => e.Record.Collected).Take(3).ToList(); 
                break;
            case "Melon": 
                leaderboard.Melon.Add(entry);
                leaderboard.Melon = leaderboard.Melon.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Mushroom": 
                leaderboard.Mushroom.Add(entry);
                leaderboard.Mushroom = leaderboard.Mushroom.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Nether Wart": 
                leaderboard.Wart.Add(entry);
                leaderboard.Wart = leaderboard.Wart.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Potato": 
                leaderboard.Potato.Add(entry);
                leaderboard.Potato = leaderboard.Potato.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Pumpkin": 
                leaderboard.Pumpkin.Add(entry);
                leaderboard.Pumpkin = leaderboard.Pumpkin.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Sugar Cane": 
                leaderboard.Cane.Add(entry);
                leaderboard.Cane = leaderboard.Cane.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
            case "Wheat": 
                leaderboard.Wheat.Add(entry);
                leaderboard.Wheat = leaderboard.Wheat.OrderByDescending(e => e.Record.Collected).Take(3).ToList();
                break;
        }

        await _context.SaveChangesAsync();
        
        return Ok(leaderboard);
    }
    
}