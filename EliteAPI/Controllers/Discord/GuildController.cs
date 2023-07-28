using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
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
    
    // GET <GuildController>/[guildId]
    [HttpGet("{guildId:long}")]
    public async Task<ActionResult<PublicGuildDto>> GetGuildById(long guildId) {
        if (guildId <= 0) return BadRequest("Invalid guild ID.");
        
        var guild = await _context.Guilds.FindAsync((ulong) guildId);
        if (guild is null) return NotFound("Guild not found.");
        
        return Ok(_mapper.Map<PublicGuildDto>(guild));
    }
}