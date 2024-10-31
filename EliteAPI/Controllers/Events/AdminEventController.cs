using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Events; 

[Authorize]
[ApiController, ApiVersion(1.0)]
[Route("/guild/{guildId}/events")]
[Route("/v{version:apiVersion}/guild/{guildId}/events")]
public class AdminEventController(
    DataContext context,
    IMapper mapper,
    IDiscordService discordService,
    IEventTeamService teamService,
    IObjectStorageService objectStorageService,
    IEventService eventService)
    : ControllerBase
{
    
    /// <summary>
    /// Get events for a guild (admin)
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpGet("admin")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventDetailsDto>>> GetGuildEvents(ulong guildId) {
        var events = await context.Events
            .Where(e => e.GuildId == guildId)
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .ToListAsync();

        return mapper.Map<List<EventDetailsDto>>(events) ?? [];
    }

    /// <summary>
    /// Get event (admin)
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpGet("{eventId}/admin")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> GetGuildEvent(ulong guildId, ulong eventId) {
        var @event = await context.Events
            .Where(e => e.GuildId == guildId && e.Id == eventId) 
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        
        if (@event is null) {
            return NotFound("Event not found.");
        }

        return mapper.Map<EventDetailsDto>(@event);
    }
    
    /// <summary>
    /// Create a Farming Weight Event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="incoming"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost("weight")]
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
    [HttpPost("medals")]
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
    [HttpPatch("{eventId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> EditEvent(ulong guildId, ulong eventId, [FromBody] EditEventDto incoming)
    {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null || eliteEvent.GuildId != guildId) return NotFound("Event not found.");

        var startTime = incoming.StartTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.StartTime.Value) : (DateTimeOffset?)null;
        var endTime = incoming.EndTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.EndTime.Value) : (DateTimeOffset?)null;
        var joinTime = incoming.JoinTime is not null ? DateTimeOffset.FromUnixTimeSeconds(incoming.JoinTime.Value) : (DateTimeOffset?)null;
        
        eliteEvent.Name = incoming.Name ?? eliteEvent.Name;
        eliteEvent.Description = incoming.Description ?? eliteEvent.Description;
        eliteEvent.JoinUntilTime = joinTime ?? eliteEvent.JoinUntilTime;
        eliteEvent.DynamicStartTime = incoming.DynamicStartTime ?? eliteEvent.DynamicStartTime;
        eliteEvent.Active = incoming.Active ?? eliteEvent.Active;
        eliteEvent.Rules = incoming.Rules ?? eliteEvent.Rules;
        eliteEvent.PrizeInfo = incoming.PrizeInfo ?? eliteEvent.PrizeInfo;
        eliteEvent.RequiredRole = incoming.RequiredRole ?? eliteEvent.RequiredRole;
        eliteEvent.BlockedRole = incoming.BlockedRole ?? eliteEvent.BlockedRole;

        // Update all related event members if the start or end time has changed
        var updateStart = startTime is not null && startTime != eliteEvent.StartTime;
        var updateEnd = endTime is not null && endTime != eliteEvent.EndTime;
        
        eliteEvent.StartTime = startTime ?? eliteEvent.StartTime;
        eliteEvent.EndTime = endTime ?? eliteEvent.EndTime;
        
        await context.SaveChangesAsync();
        
        if (updateStart || updateEnd) {
            await context.EventMembers
                .Where(em => em.EventId == eventId)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(e => e.StartTime, eliteEvent.StartTime)
                    .SetProperty(e => e.EndTime, eliteEvent.EndTime));
        }

        return Ok();
    }

    /// <summary>
    /// Set event banner image
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost("{eventId}/banner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> SetEventBanner(ulong guildId, ulong eventId, [FromForm] EditEventBannerDto data)
    {
        if (data.Image is null) return BadRequest("No image provided.");
        
        var eliteEvent = await context.Events
            .Include(e => e.Banner)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");

        var newImage = await objectStorageService.UploadImageAsync($"guilds/{guildId}/events/{eventId}/banner.png", data.Image);
        
        if (eliteEvent.Banner is not null) {
            eliteEvent.Banner.Metadata = newImage.Metadata;
            eliteEvent.Banner.Hash = newImage.Hash;
            eliteEvent.Banner.Title = newImage.Title;
            eliteEvent.Banner.Description = newImage.Description;
        } else {
            context.Images.Add(newImage);
            eliteEvent.Banner = newImage;
        }
        
        await context.SaveChangesAsync();

        return Ok();
    }
    
    /// <summary>
    /// Delete event banner image
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="image"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete("{eventId}/banner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> DeleteEventBanner(ulong guildId, ulong eventId)
    {
        var eliteEvent = await context.Events
            .Include(e => e.Banner)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");

        if (eliteEvent.Banner is null) return Ok();
        
        await objectStorageService.DeleteAsync(eliteEvent.Banner.Path);
        context.Images.Remove(eliteEvent.Banner);
        eliteEvent.Banner = null;
        eliteEvent.BannerId = null;
            
        await context.SaveChangesAsync();

        return Ok();
    }


    /// <summary>
    /// Delete an Event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete("{eventId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> DeleteEvent(ulong guildId, ulong eventId)
    {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");

        context.Events.Remove(eliteEvent);
        await context.SaveChangesAsync();
        
        return Ok(eliteEvent);
    }
    
    /// <summary>
    /// Get all members from an event
    /// </summary>
    /// <remarks>
    /// This differs from the normal GetEventMembers route in that it includes members who aren't on a team in a team event
    /// </remarks>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpGet("{eventId}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<AdminEventMemberDto>>> GetEventMembers(ulong guildId, ulong eventId)
    {
        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");

        var members = await context.EventMembers
            .Include(m => m.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount).AsNoTracking()
            .Where(em => em.EventId == eventId &&
                         (em.Status == EventMemberStatus.Active || em.Status == EventMemberStatus.Inactive))
            .ToListAsync();
        
        var mapped = mapper.Map<List<AdminEventMemberDto>>(members);
        
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get banned members from an event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpGet("{eventId}/bans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberBannedDto>>> GetBannedMembers(ulong guildId, ulong eventId)
    {
        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var members = await context.EventMembers
            .Include(m => m.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount).AsNoTracking()
            .Where(em => em.EventId == eventId &&
                         (em.Status == EventMemberStatus.Disqualified || em.Status == EventMemberStatus.Left))
            .ToListAsync();
        
        var mapped = mapper.Map<List<EventMemberBannedDto>>(members);
        
        return Ok(mapped);
    }

    /// <summary>
    /// Force add a member to an event
    /// </summary>
    /// <remarks>
    /// Use with caution, this will add a member to an event without checking if they meet the requirements or if the event is running or not.
    /// </remarks>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <param name="profileId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost("{eventId}/members/{playerUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<IActionResult> ForceAddMember(ulong guildId, ulong eventId, string playerUuid, [FromQuery] string profileId)
    {
        if (playerUuid.Length != 32 || string.IsNullOrWhiteSpace(profileId) || profileId.Length != 32) {
            return BadRequest("Invalid player UUID.");
        }
        
        var @event = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        
        if (@event is null) {
            return NotFound("Event not found.");
        }
        
        var member = await context.ProfileMembers.AsNoTracking()
            .Include(p => p.MinecraftAccount)
            .Where(p => p.PlayerUuid == playerUuid && p.ProfileId == profileId)
            .FirstOrDefaultAsync();

        if (member?.MinecraftAccount.AccountId is null) {
            return BadRequest("Player not found, or player does not have a linked account.");
        }

        var existing = await context.EventMembers.AsNoTracking()
            .Where(em => em.EventId == eventId && em.ProfileMemberId == member.Id)
            .FirstOrDefaultAsync();
        
        if (existing is not null) {
            return BadRequest("Player is already in the event.");
        }
        
        await eventService.CreateEventMember(@event, new CreateEventMemberDto {
            EventId = @event.Id,
            ProfileMemberId = member.Id,
            UserId = member.MinecraftAccount.AccountId.Value,
            Score = 0,
            StartTime = @event.StartTime,
            EndTime = @event.EndTime,
            ProfileMember = member
        });
        
        await context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Fully delete a member record from an event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <param name="profileId"></param>
    /// <param name="recordId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete("{eventId}/members/{playerUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<IActionResult> PermDeleteMember(ulong guildId, ulong eventId, string playerUuid, [FromQuery] string? profileId = null, [FromQuery] int recordId = -1)
    {
        var @event = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (@event is null) return NotFound("Event not found.");
        
        if (playerUuid.Length != 32) {
            return BadRequest("Invalid player UUID.");
        }
        
        var member = (recordId != -1) 
            ? await context.EventMembers
                .Include(m => m.ProfileMember)
                .Where(em => em.EventId == eventId
                             && em.ProfileMember.PlayerUuid == playerUuid
                             && (profileId == null || em.ProfileMember.ProfileId == profileId))
                .FirstOrDefaultAsync()
            : await context.EventMembers
                .Include(m => m.ProfileMember)
                .Where(em => em.EventId == eventId && em.Id == recordId)
                .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        context.EventMembers.Remove(member);

        return Ok();
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
    [HttpPost("{eventId}/bans/{playerUuid}")]
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
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var member = await context.EventMembers
            .Include(m => m.ProfileMember)
            .Where(em => em.EventId == eventId && em.ProfileMember.PlayerUuid == playerUuid)
            .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        member.Status = EventMemberStatus.Disqualified;
        member.TeamId = null;
        member.Team = null;
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
    [HttpDelete("{eventId}/bans/{playerUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> UnbanMember(ulong guildId, ulong eventId, string playerUuid)
    {
        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var member = await context.EventMembers
            .Include(m => m.ProfileMember)
            .Where(em => em.EventId == eventId && em.ProfileMember.PlayerUuid == playerUuid)
            .FirstOrDefaultAsync();
        
        if (member is null) {
            return NotFound("Member not found.");
        }
        
        member.Status = EventMemberStatus.Active;
        member.TeamId = null;
        member.Team = null;
 
        await context.SaveChangesAsync();
        return Ok();
    }
    
    /// <summary>
    /// Get all teams in an event
    /// </summary>
    /// <remarks>
    /// This is a protected route in order to include join codes for the teams
    /// </remarks>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="teamId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpGet("{eventId}/teams")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventTeamWithMembersDto>>> GetTeams(ulong guildId, ulong eventId, int teamId)
    {
        var @event = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GuildId == guildId);
        if (@event is null) return NotFound("Event not found.");
        
        var teams = await teamService.GetEventTeamsAsync(eventId);
        var mapped = teams.Select(t => new EventTeamWithMembersDto {
            EventId = t.EventId.ToString(),
            Id = t.Id,
            Name = t.Name,
            Score = t.Members.Sum(m => m.Score).ToString(CultureInfo.InvariantCulture),
            JoinCode = t.JoinCode,
            OwnerId = t.UserId,
            Members = mapper.Map<List<EventMemberDto>>(t.Members)
        }).ToList();
        
        return mapped;
    }

    /// <summary>
    /// Create a team for an event
    /// </summary>
    /// <remarks>
    /// This generally should only be used for events with a set amount of teams (users are not allowed to create their own teams)
    /// </remarks>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="incoming"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost("{eventId}/teams")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> CreateTeam(ulong guildId, ulong eventId, [FromBody] CreateEventTeamDto incoming) {
        var userId = User.GetId();
        if (userId is null) {
            return Unauthorized();
        }
        return await teamService.CreateAdminTeamAsync(eventId, incoming, userId);
    }
    
    /// <summary>
    /// Delete a team from an event
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="teamId"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete("{eventId}/teams/{teamId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> DeleteTeam(ulong guildId, ulong eventId, int teamId)
    {
        return await teamService.DeleteTeamAsync(teamId);
    }

    /// <summary>
    /// Remove a member from a team
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="teamId"></param>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpDelete("{eventId}/teams/{teamId}/members/{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> KickTeamMember(ulong guildId, ulong eventId, int teamId, string playerUuidOrIgn)
    {
        return await teamService.KickMemberAsync(teamId, playerUuidOrIgn);
    }
    
    /// <summary>
    /// Add a member to a team
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="eventId"></param>
    /// <param name="teamId"></param>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    [GuildAdminAuthorize]
    [HttpPost("{eventId}/teams/{teamId}/members/{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> AddTeamMember(ulong guildId, ulong eventId, int teamId, string playerUuidOrIgn)
    {
        return await teamService.AddMemberToTeamAsync(teamId, playerUuidOrIgn);
    }
}