using System.Net.Mime;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.EventService;
using EliteAPI.Services.GuildService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Events; 

[Route("/Event")]
[ServiceFilter(typeof(DiscordAuthFilter))]
[ApiController]
public class AdminEventController(
    DataContext context,
    IMapper mapper,
    IDiscordService discordService,
    IGuildService guildService,
    IEventService eventService)
    : ControllerBase 
{
    /// <summary>
    /// Create a Farming Weight Event
    /// </summary>
    /// <param name="incoming"></param>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/create
    [Route("/Event/Create")]
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> CreateEvent([FromBody] CreateWeightEventDto incoming) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        if (!ulong.TryParse(incoming.GuildId, out var guildId)) {
            return BadRequest("Invalid guild ID.");
        }

        var canCreate = await CanCreateEvent(incoming, account, token);
        if (canCreate is not OkResult) {
            return canCreate;
        }

        var result = await eventService.CreateEvent(incoming, guildId, account.Id);

        if (result.Value is null) {
            return BadRequest(result.Result);
        }

        return Ok(mapper.Map<EventDetailsDto>(result.Value));
    }
    
    /// <summary>
    /// Create a Medal Collection Event
    /// </summary>
    /// <param name="incoming"></param>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/create
    [Route("/Event/Create/Medals")]
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> CreateMedalEvent([FromBody] CreateMedalEventDto incoming) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        if (!ulong.TryParse(incoming.GuildId, out var guildId)) {
            return BadRequest("Invalid guild ID.");
        }

        var canCreate = await CanCreateEvent(incoming, account, token);
        if (canCreate is not OkResult) {
            return canCreate;
        }

        var result = await eventService.CreateEvent(incoming, guildId, account.Id);

        if (result.Value is null) {
            return BadRequest(result.Result);
        }

        return Ok(mapper.Map<EventDetailsDto>(result.Value));
    }

    private async Task<ActionResult> CanCreateEvent(CreateEventDto incoming, EliteAccount account, string token) {
        if (!ulong.TryParse(incoming.GuildId, out var guildId)) {
            return BadRequest("Invalid guild ID.");
        }
        
        await discordService.RefreshBotGuilds();
        
        var guilds = await discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == incoming.GuildId);

        if (userGuild is null || !guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server in order to edit it!");
        }

        var guild = await context.Guilds.AsNoTracking().FirstOrDefaultAsync(g => g.Id == guildId);
        
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guild.Features.EventsEnabled) {
            return BadRequest("This guild does not have access to make events!");
        }
        
        if (guild.Features.EventSettings is not null) {
            // Check if the guild has reached their max amount of events for the month
            var startOfMonth = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var count = guild.Features.EventSettings.CreatedEvents.Count(e => e.CreatedAt > startOfMonth);
            if (count >= guild.Features.EventSettings.MaxMonthlyEvents) {
                return BadRequest("You have reached your maximum amount of events for this month!");
            }
        }

        return Ok();
    }

    /// <summary>
    /// Edit an Event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="incoming"></param>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/edit
    [HttpPost("{eventId}/Edit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> EditEvent(ulong eventId, [FromBody] EditEventDto incoming)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !guildService.HasGuildAdminPermissions(userGuild)) {
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
        
        await context.SaveChangesAsync();

        // Update all related event members if the start or end time has changed
        
        var updateStart = startTime is not null && startTime != eliteEvent.StartTime;
        var updateEnd = endTime is not null && endTime != eliteEvent.EndTime;
        
        if (updateStart || updateEnd) {
            await context.EventMembers
                .Where(em => em.EventId == eventId)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(e => e.StartTime, startTime)
                    .SetProperty(e => e.EndTime, endTime));
        }
        
        return Ok(eliteEvent);
    }
    
    /// <summary>
    /// Get banned members from an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // GET <EventController>/12793764936498429/bans
    [HttpGet("{eventId}/bans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberBannedDto>>> GetBannedMembers(ulong eventId)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server!");
        }
        
        var members = await context.EventMembers
            .Include(m => m.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount).AsNoTracking()
            .Where(em => em.EventId == eventId && em.Status == EventMemberStatus.Disqualified || em.Status == EventMemberStatus.Left)
            .ToListAsync();
        
        var mapped = mapper.Map<List<EventMemberBannedDto>>(members);
        
        return Ok(mapped);
    }
    
    /// <summary>
    /// Ban a member from an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
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
        
        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server!");
        }
        
        var member = await context.EventMembers
            .Include(m => m.ProfileMember)
            .Where(em => em.EventId == eventId && em.ProfileMember.PlayerUuid == playerUuid)
            .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        member.Status = EventMemberStatus.Disqualified;
        member.Notes = reason;
        
        await context.SaveChangesAsync();
        
        return Ok(mapper.Map<EventMemberBannedDto>(member));
    }
    
    /// <summary>
    /// Unban a member from an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    // DELETE <EventController>/12793764936498429/bans
    [HttpDelete("{eventId}/bans/{playerUuid:length(32)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> UnbanMember(ulong eventId, string playerUuid)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var guilds = await discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null || !guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You need to have admin permissions in the event's Discord server!");
        }
        
        var member = await context.EventMembers
            .Include(m => m.ProfileMember)
            .Where(em => em.EventId == eventId && em.ProfileMember.PlayerUuid == playerUuid)
            .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        member.Status = EventMemberStatus.Active;
 
        await context.SaveChangesAsync();
        
        return Ok();
    }
}