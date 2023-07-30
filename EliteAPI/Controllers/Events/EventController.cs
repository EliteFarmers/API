using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Events; 

[Route("[controller]/{eventId}")]
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
    
    // GET <EventController>/12793764936498429
    [HttpGet]
    public async Task<ActionResult<EventDetailsDto>> Get(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return BadRequest("Invalid event ID.");
        
        await _discordService.RefreshBotGuilds();
        
        var @event = await _context.Events
            .FirstOrDefaultAsync(e => e.Id.Equals(eventId));
        if (@event is null) return NotFound("Event not found.");
        

        return Ok(@event);
    }
    
    // GET <EventController>/12793764936498429/players
    
    
}