using System.Net.Mime;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Events; 

[Authorize]
[ApiController, ApiVersion(1.0)]
[Route("/event")]
[Route("/v{version:apiVersion}/event")]
public class AdminEventController(
    DataContext context,
    IMapper mapper,
    IDiscordService discordService,
    IGuildService guildService,
    IEventService eventService,
    UserManager<ApiUser> userManager)
    : ControllerBase
{
    /// <summary>
    /// Create a Farming Weight Event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="incoming"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost, Route("/guild/{guildId}/events/weight")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> CreateEvent(ulong guildId, [FromBody] CreateWeightEventDto incoming) {
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        var canCreate = CanCreateEvent(guild);
        if (canCreate is not OkResult) {
            return canCreate;
        }

        var result = await eventService.CreateEvent(incoming, guildId);

        if (result.Value is null) {
            return BadRequest(result.Result);
        }

        return Ok(mapper.Map<EventDetailsDto>(result.Value));
    }

    /// <summary>
    /// Create a Medal Collection Event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="incoming"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost, Route("/guild/{guildId}/events/medals")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> CreateMedalEvent(ulong guildId, [FromBody] CreateMedalEventDto incoming) {
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        var canCreate = CanCreateEvent(guild);
        if (canCreate is not OkResult) {
            return canCreate;
        }
        
        var result = await eventService.CreateEvent(incoming, guildId);

        if (result.Value is null) {
            return BadRequest(result.Result);
        }

        return Ok(mapper.Map<EventDetailsDto>(result.Value));
    }

    private ActionResult CanCreateEvent(Guild guild) {
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
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="incoming"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPatch, Route("/guild/{guildId}/events/{eventId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> EditEvent(ulong guildId, ulong eventId, [FromBody] EditEventDto incoming)
    {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");

        var startTime = incoming.StartTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.StartTime.Value) : (DateTimeOffset?)null;
        var endTime = incoming.EndTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.EndTime.Value) : (DateTimeOffset?)null;
        var joinTime = incoming.JoinTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.JoinTime.Value) : (DateTimeOffset?)null;
        
        eliteEvent.Name = incoming.Name ?? eliteEvent.Name;
        eliteEvent.Description = incoming.Description ?? eliteEvent.Description;
        eliteEvent.StartTime = startTime ?? eliteEvent.StartTime;
        eliteEvent.EndTime = endTime ?? eliteEvent.EndTime;
        eliteEvent.JoinUntilTime = joinTime ?? eliteEvent.JoinUntilTime;
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
    /// Delete an Event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete, Route("/guild/{guildId}/events/{eventId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> DeleteEvent(ulong guildId, ulong eventId)
    {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");

        context.Events.Remove(eliteEvent);
        await context.SaveChangesAsync();
        
        return Ok(eliteEvent);
    }
    
    /// <summary>
    /// Get banned members from an event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpGet, Route("/guild/{guildId}/events/{eventId}/bans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberBannedDto>>> GetBannedMembers(ulong guildId, ulong eventId)
    {
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
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost, Route("/guild/{guildId}/events/{eventId}/bans/{playerUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberBannedDto>>> BanMember(ulong guildId, ulong eventId, string playerUuid, [FromBody] string reason)
    {
        if (playerUuid.Length != 32) {
            return BadRequest("Invalid player UUID.");
        }
        
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 128) {
            return BadRequest("Invalid reason.");
        }
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
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
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete, Route("/guild/{guildId}/events/{eventId}/bans/{playerUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> UnbanMember(ulong guildId, ulong eventId, string playerUuid)
    {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
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