﻿using System.Net.Mime;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.EventService;
using EliteAPI.Services.MemberService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Controllers.Events; 

[Route("[controller]/{eventId}")]
[ApiController]
public class EventController(
    DataContext context,
    IMapper mapper,
    IMemberService memberService,
    IDiscordService discordService,
    IEventService eventService)
    : ControllerBase 
{

    /// <summary>
    /// Get all upcoming events
    /// </summary>
    /// <returns></returns>
    // GET <EventController>s/
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventDetailsDto>>> GetUpcoming()
    {
        await discordService.RefreshBotGuilds();

        var events = await eventService.GetUpcomingEvents();
        
        return Ok(events);
    }
    
    /// <summary>
    /// Get an event by ID
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // GET <EventController>/12793764936498429
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> GetEvent(ulong eventId)
    {
        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        return Ok(mapper.Map<EventDetailsDto>(eliteEvent));
    }
    
    /// <summary>
    /// Get members of an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // GET <EventController>/12793764936498429
    [HttpGet("members")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberDetailsDto>>> GetEventMembers(ulong eventId)
    {
        await discordService.RefreshBotGuilds();

        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");

        var members = await context.EventMembers.AsNoTracking()
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .AsNoTracking()
            .Where(e => e.EventId == eventId 
                        && e.Status != EventMemberStatus.Disqualified 
                        && e.Status != EventMemberStatus.Left)
            .OrderByDescending(e => e.Score)
            .ToListAsync();

        var mapped = members.Select(mapper.Map<EventMemberDetailsDto>);
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get a member of an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    // GET <EventController>/12793764936498429
    [HttpGet("member/{playerUuid}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<EventMemberDto>>> GetEventMember(ulong eventId, string playerUuid)
    {
        var uuid = playerUuid.Replace("-", "");
        if (uuid is not { Length: 32 }) return BadRequest("Invalid playerUuid");
        
        await discordService.RefreshBotGuilds();
        
        var member = await context.EventMembers.AsNoTracking()
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.ProfileMember.PlayerUuid == uuid);
        if (member is null) return NotFound("Event member not found.");
        
        var mapped = mapper.Map<EventMemberDto>(member);

        return Ok(mapped);
    }
    
    /// <summary>
    /// Join an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <param name="profileId"></param>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/join
    [HttpPost("join")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> JoinEvent(ulong eventId, [FromQuery] string? playerUuid, [FromQuery] string? profileId)
    {
        if (HttpContext.Items["Account"] is not EliteAccount authAccount || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }
        
        var account = await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id == authAccount.Id);
        
        if (account is null) {
            return Unauthorized("Account not found.");
        }
        
        if (playerUuid is not null && playerUuid.Length != 32) {
            return BadRequest("Invalid playerUuid provided.");
        }
        
        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        if (DateTimeOffset.UtcNow > eliteEvent.JoinUntilTime || DateTimeOffset.UtcNow > eliteEvent.EndTime) {
            return BadRequest("You can no longer join this event.");
        }
        
        var guilds = await discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null) {
            return NotFound("You need to be in the event's Discord server in order to join!");
        }
        
        var selectedAccount = (playerUuid.IsNullOrEmpty()) 
            ? account.MinecraftAccounts.FirstOrDefault(a => a.Selected)
            : account.MinecraftAccounts.FirstOrDefault(a => a.Id == playerUuid);
        
        if (selectedAccount is null) {
            return NotFound("You need to have a linked Minecraft account in order to join!");
        }
        
        var member = await context.EventMembers.AsNoTracking()
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.ProfileMember.PlayerUuid == selectedAccount.Id);
        
        if (member is not null && member.Status != EventMemberStatus.Left) {
            return BadRequest("You are already a member of this event!");
        }

        var updateQuery = await memberService.ProfileMemberQuery(selectedAccount.Id);

        if (updateQuery is null) {
            return BadRequest("Profile member data not found, please try again later or ensure an existing profile is selected.");
        }

        var query = updateQuery
            .Include(p => p.MinecraftAccount).AsNoTracking()
            .Include(p => p.Farming).AsNoTracking()
            .Where(p => p.PlayerUuid == selectedAccount.Id);
            
        var profileMember = (profileId.IsNullOrEmpty()) 
            ? await query.FirstOrDefaultAsync(a => a.IsSelected)
            : await query.FirstOrDefaultAsync(a => a.ProfileId == profileId);

        if (profileMember?.Farming is null) {
            return BadRequest("Profile member data not found, please try again later or ensure an existing profile is selected.");
        }
        
        if (!profileMember.Api.Collections) {
            return BadRequest("Collections API needs to be enabled in order to join this event.");
        }

        if (eliteEvent.Type == EventType.FarmingWeight) {
            if (!profileMember.Api.Inventories) {
                return BadRequest("Inventories API needs to be enabled in order to join this event.");
            }
            
            if (profileMember.Farming.Inventory?.Tools is null || profileMember.Farming.Inventory.Tools.Count == 0) {
                return BadRequest("You need to have at least one farming tool in your inventory in order to join this event.");
            }
        }
        
        var eventActive = eliteEvent.StartTime < DateTimeOffset.UtcNow && eliteEvent.EndTime > DateTimeOffset.UtcNow;
        if (eventActive != eliteEvent.Active) {
            eliteEvent.Active = eventActive;
        }
        
        var newMember = member ?? new EventMember {
            EventId = eliteEvent.Id,
            Type = eliteEvent.Type,
            
            ProfileMemberId = profileMember.Id,
            Score = 0,
            
            LastUpdated = DateTimeOffset.UtcNow,
            StartTime = eliteEvent.StartTime,
            EndTime = eliteEvent.EndTime,
            
            UserId = account.Id
        };

        newMember.Status = eventActive ? EventMemberStatus.Active : EventMemberStatus.Inactive;

        if (eliteEvent is { Type: EventType.FarmingWeight, Active: true } && DateTimeOffset.FromUnixTimeSeconds(profileMember.LastUpdated) > eliteEvent.StartTime) {
            // The event has already started, initialize the event member with the current data
            ((WeightEventMember) newMember).Initialize(profileMember);
        }
        
        profileMember.EventEntries ??= new List<EventMember>();
        
        if (member is null) {
            profileMember.EventEntries.Add(newMember);
            context.EventMembers.Add(newMember);
        }
        else {
            context.Entry(newMember).State = EntityState.Modified;
        }
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Leave an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/leave
    [HttpPost("leave")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> LeaveEvent(ulong eventId)
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        await discordService.RefreshBotGuilds();
        
        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var members = await context.EventMembers
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .Where(e => e.EventId == eventId && e.ProfileMember.MinecraftAccount.AccountId == account.Id)
            .ToListAsync();
        
        if (members is not { Count: > 0 } ) {
            return BadRequest("You are not a member of this event!");
        }
        
        if (DateTimeOffset.UtcNow > eliteEvent.EndTime) {
            return BadRequest("You can no longer leave this event as it has ended.");
        }

        foreach (var member in members)
        {
            member.Status = EventMemberStatus.Left;
        }

        await context.SaveChangesAsync();
        
        return Ok();
    }
}