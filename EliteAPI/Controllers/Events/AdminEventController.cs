using System.Net.Mime;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.GuildService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Events; 

[Route("/Event")]
[ServiceFilter(typeof(DiscordAuthFilter))]
[ApiController]
public class AdminEventController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IDiscordService _discordService;
    private readonly IGuildService _guildService;
    
    public AdminEventController(DataContext context, IMapper mapper, IDiscordService discordService, IGuildService guildService)
    {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
        _guildService = guildService;
    }
    
    // POST <EventController>/12793764936498429/create
    [Route("/Event/Create")]
    [HttpPost("create")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> CreateEvent([FromBody] EditEventDto incoming) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        if (!ulong.TryParse(incoming.GuildId, out var guildId)) {
            return BadRequest("Invalid guild ID.");
        }
        
        await _discordService.RefreshBotGuilds();
        
        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == incoming.GuildId);

        if (userGuild is null || !_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server in order to edit it!");
        }

        var guild = await _context.Guilds.FindAsync(guildId);
        
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guild.Features.EventsEnabled) {
            return BadRequest("This guild does not have access to make events!");
        }
        
        guild.Features.EventSettings ??= new GuildEventSettings();

        // Check if the user has reached their max amount of events for the month
        var startOfMonth = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var count = guild.Features.EventSettings.CreatedEvents.Count(e => e.CreatedAt > startOfMonth);
        if (count >= guild.Features.EventSettings.MaxMonthlyEvents) {
            return BadRequest("You have reached your max amount of events for this month!");
        }
        
        var startTime = incoming.StartTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.StartTime.Value) : (DateTimeOffset?)null;
        var endTime = incoming.EndTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.EndTime.Value) : (DateTimeOffset?)null;

        var eliteEvent = new Event {
            Id = guildId + (ulong) new Random().Next(100000000, 999999999),
            
            Name = incoming.Name ?? "Untitled Event",
            Description = incoming.Description,
            Rules = incoming.Rules,
            PrizeInfo = incoming.PrizeInfo,
            Public = true, // For now, all events are public
            
            Banner = incoming.Banner,
            Thumbnail = incoming.Thumbnail,
            
            StartTime = startTime ?? DateTimeOffset.UtcNow.AddDays(1),
            EndTime = endTime ?? DateTimeOffset.UtcNow.AddDays(8),
            DynamicStartTime = incoming.DynamicStartTime ?? false,
            Active = incoming.Active ?? false,
            
            RequiredRole = incoming.RequiredRole,
            BlockedRole = incoming.BlockedRole,
            
            OwnerId = account.Id,
            GuildId = guildId
        };
        
        guild.Features.EventSettings.CreatedEvents.Add(new EventCreatedDto {
            Id = eliteEvent.Id.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        });
        _context.Guilds.Update(guild);
        
        await _context.Events.AddAsync(eliteEvent);
        await _context.SaveChangesAsync();
        
        return Ok(_mapper.Map<EventDetailsDto>(eliteEvent));
    }

    // POST <EventController>/12793764936498429/edit
    [HttpPost("{eventId}/edit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> EditEvent(ulong eventId, [FromBody] EditEventDto incoming)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        await _discordService.RefreshBotGuilds();
        
        var eliteEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server in order to edit it!");
        }

        var startTime = incoming.StartTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.StartTime.Value) : (DateTimeOffset?)null;
        var endTime = incoming.EndTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.EndTime.Value) : (DateTimeOffset?)null;
        
        eliteEvent.Name = incoming.Name ?? eliteEvent.Name;
        eliteEvent.Description = incoming.Description ?? eliteEvent.Description;
        eliteEvent.StartTime = startTime ?? eliteEvent.StartTime;
        eliteEvent.EndTime = endTime ?? eliteEvent.EndTime;
        eliteEvent.DynamicStartTime = incoming.DynamicStartTime ?? eliteEvent.DynamicStartTime;
        eliteEvent.Active = incoming.Active ?? eliteEvent.Active;
        eliteEvent.Rules = incoming.Rules ?? eliteEvent.Rules;
        eliteEvent.PrizeInfo = incoming.PrizeInfo ?? eliteEvent.PrizeInfo;
        eliteEvent.Banner = incoming.Banner ?? eliteEvent.Banner;
        eliteEvent.Thumbnail = incoming.Thumbnail ?? eliteEvent.Thumbnail;
        eliteEvent.RequiredRole = incoming.RequiredRole ?? eliteEvent.RequiredRole;
        eliteEvent.BlockedRole = incoming.BlockedRole ?? eliteEvent.BlockedRole;
        
        await _context.SaveChangesAsync();
        
        return Ok(eliteEvent);
    }
    
    // POST <EventController>/12793764936498429/bans
    [HttpGet("{eventId}/bans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberBannedDto>>> GetBannedMembers(ulong eventId)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        await _discordService.RefreshBotGuilds();
        
        var eliteEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server!");
        }
        
        var members = await _context.EventMembers
            .Include(m => m.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount).AsNoTracking()
            .Where(em => em.EventId == eventId && em.Status == EventMemberStatus.Disqualified || em.Status == EventMemberStatus.Left)
            .ToListAsync();
        
        var mapped = _mapper.Map<List<EventMemberBannedDto>>(members);
        
        return Ok(mapped);
    }
    
    // POST <EventController>/12793764936498429/bans/12345678901234567890123456789012
    [HttpPost("{eventId}/bans/{playerUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberBannedDto>>> BanMember(ulong eventId, string playerUuid, [FromBody] string reason)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        if (playerUuid.Length != 32) {
            return BadRequest("Invalid player UUID.");
        }
        
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 128) {
            return BadRequest("Invalid reason.");
        }
        
        await _discordService.RefreshBotGuilds();
        
        var eliteEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server!");
        }
        
        var member = await _context.EventMembers
            .Include(m => m.ProfileMember)
            .Where(em => em.EventId == eventId && em.ProfileMember.PlayerUuid == playerUuid)
            .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        member.Status = EventMemberStatus.Disqualified;
        member.Notes = reason;
        
        await _context.SaveChangesAsync();
        
        return Ok(_mapper.Map<EventMemberBannedDto>(member));
    }
    
    // POST <EventController>/12793764936498429/bans
    [HttpDelete("{eventId}/bans/{playerUuid:length(32)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> UnbanMember(ulong eventId, string playerUuid)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        await _discordService.RefreshBotGuilds();
        
        var eliteEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server!");
        }
        
        var member = await _context.EventMembers
            .Include(m => m.ProfileMember)
            .Where(em => em.EventId == eventId && em.ProfileMember.PlayerUuid == playerUuid)
            .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        member.Status = EventMemberStatus.Active;
 
        await _context.SaveChangesAsync();
        
        return Ok();
    }
}