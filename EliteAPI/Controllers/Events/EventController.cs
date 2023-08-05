using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Events; 

[Route("[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IDiscordService _discordService;

    public EventController(DataContext context, IMapper mapper, IDiscordService discordService)
    {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
    }
    
    // GET <EventController>s/
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<List<EventDetailsDto>>> GetUpcoming()
    {
        await _discordService.RefreshBotGuilds();
        
        var events = await _context.Events
            .Where(e => e.EndTime > DateTimeOffset.UtcNow)
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .ToListAsync();
        
        var eventDetails = _mapper.Map<List<EventDetailsDto>>(events);
        
        return Ok(eventDetails);
    }
    
    // GET <EventController>/12793764936498429
    [HttpGet("{eventId}")]
    public async Task<ActionResult<EventDetailsDto>> Get(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return BadRequest("Invalid event ID.");
        
        await _discordService.RefreshBotGuilds();
        
        var eliteEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id.Equals(eventId));
        if (eliteEvent is null) return NotFound("Event not found.");
        

        return Ok(eliteEvent);
    }
}