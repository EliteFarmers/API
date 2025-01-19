using System.Net.Mime;
using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Controllers.Events; 

[ApiController, ApiVersion(1.0)]
[Route("[controller]/{eventId}")]
[Route("/v{version:apiVersion}/[controller]/{eventId}")]
public class EventController(
    DataContext context,
    IMapper mapper,
    IMemberService memberService,
    IDiscordService discordService,
    IEventService eventService,
    IConnectionMultiplexer redis,
    UserManager<ApiUser> userManager)
    : ControllerBase 
{

    /// <summary>
    /// Get all upcoming events
    /// </summary>
    /// <returns></returns>
    // GET <EventController>s/
    [HttpGet]
    [Route("/[controller]s")]
    [Route("/v{version:apiVersion}/[controller]s")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventDetailsDto>>> GetUpcoming()
    {
        var events = await eventService.GetUpcomingEvents();
        
        return Ok(events);
    }
    
    /// <summary>
    /// Get all upcoming events
    /// </summary>
    /// <returns></returns>
    // GET <EventController>s/
    [HttpGet]
    [Route("/[controller]/defaults")]
    [Route("/v{version:apiVersion}/[controller]/defaults")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<EventDefaultsDto> GetEventDefaults()
    {
        var defaults = new EventDefaultsDto {
            CropWeights = FarmingWeightConfig.Settings.EventCropWeights,
            // This should be moved to a config file eventually
            MedalValues = new MedalEventData().MedalWeights
        };
        
        return Ok(defaults);
    }
    
    /// <summary>
    /// Get an event by ID
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // GET <EventController>/12793764936498429
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> GetEvent(ulong eventId)
    {
        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Approved);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        var mapped = mapper.Map<EventDetailsDto>(eliteEvent);
        
        if (mapped.Data is WeightEventData { CropWeights: not { Count: > 0 } } data) {
            data.CropWeights = FarmingWeightConfig.Settings.EventCropWeights;
        }
        
        return Ok(mapper.Map<EventDetailsDto>(eliteEvent));
    }

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
        var db = redis.GetDatabase();
        var key = $"event:{eventId}:members";
        var cached = await db.StringGetAsync(key);
        
        if (cached is { IsNullOrEmpty: false, HasValue: true }) {
            var cachedMembers = JsonSerializer.Deserialize<List<EventMemberDetailsDto>>(cached!, JsonOptions);
            return Ok(cachedMembers);
        }

        var eliteEvent = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");

        var isTeamEvent = eliteEvent.GetMode() != EventTeamMode.Solo;

        var members = await context.EventMembers.AsNoTracking()
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .AsNoTracking()
            .Where(e => e.EventId == eventId 
                        && e.Status != EventMemberStatus.Disqualified 
                        && e.Status != EventMemberStatus.Left
                        && (e.TeamId != null || !isTeamEvent))
            .OrderByDescending(e => e.Score)
            .AsSplitQuery()
            .ToListAsync();

        if (eliteEvent.Type == EventType.Medals) {
            members = members.OrderByDescending(e => e as MedalEventMember).ToList();
        }

        var mapped = members.Select(mapper.Map<EventMemberDetailsDto>);
        
        var expiry = eliteEvent.Active ? TimeSpan.FromMinutes(2) : TimeSpan.FromMinutes(10);
        await db.StringSetAsync(key, JsonSerializer.Serialize(mapped, JsonOptions), expiry);
        
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get a member of an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    [OptionalAuthorize]
    [HttpGet("member/{playerUuid}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventMemberDto>> GetEventMember(ulong eventId, string playerUuid)
    {
        var uuid = playerUuid.Replace("-", "");
        if (uuid is not { Length: 32 }) return BadRequest("Invalid playerUuid");
        
        var member = await context.EventMembers.AsNoTracking()
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.ProfileMember.PlayerUuid == uuid);
        if (member is null) return NotFound("Event member not found.");
        
        var mapped = mapper.Map<EventMemberDto>(member);
        
        if (User.GetId() is { } id && (id == member.UserId.ToString() || User.IsInRole(ApiUserPolicies.Moderator))) {
            return mapped;
        } else {
            mapped.Notes = null;
            return mapped;
        }
    }

    /// <summary>
    /// Join an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="playerUuid"></param>
    /// <param name="profileId"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/join
    [HttpPost("join")]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> JoinEvent(ulong eventId, [FromQuery] string? playerUuid, [FromQuery] string? profileId) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
            return BadRequest("Linked account not found.");
        }
        
        if (user.DiscordAccessToken is null) {
            return BadRequest("Discord account not linked.");
        }
        
        if (user.AccountId is null) {
            return BadRequest("Linked account not found.");
        }
        
        await context.Entry(user).Reference(x => x.Account).LoadAsync();
        var account = user.Account;
        
        if (playerUuid is not null && playerUuid.Length != 32) {
            return BadRequest("Invalid playerUuid provided.");
        }
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eliteEvent is null) return NotFound("Event not found.");
        
        if (DateTimeOffset.UtcNow > eliteEvent.JoinUntilTime || DateTimeOffset.UtcNow > eliteEvent.EndTime) {
            return BadRequest("You can no longer join this event.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.Id);
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

        if (member is not null) {
            member.Status = eventActive ? EventMemberStatus.Active : EventMemberStatus.Inactive;
            context.Entry(member).State = EntityState.Modified;
            // Init member if needed
            await eventService.InitializeEventMember(member, profileMember);
            return Ok();
        }

        await eventService.CreateEventMember(eliteEvent, new CreateEventMemberDto {
            EventId = eliteEvent.Id,
            ProfileMemberId = profileMember.Id,
            UserId = account.Id,
            Score = 0,
            StartTime = eliteEvent.StartTime,
            EndTime = eliteEvent.EndTime,
            ProfileMember = profileMember
        });
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Leave an event
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // POST <EventController>/12793764936498429/leave
    [Authorize]
    [HttpPost("leave")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<EventDetailsDto>> LeaveEvent(ulong eventId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
            return BadRequest("Linked account not found.");
        }

        if (user.AccountId is null) {
            return BadRequest("Linked account not found.");
        }
        
        await context.Entry(user).Reference(x => x.Account).LoadAsync();
        var account = user.Account;
        
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

            if (member.TeamId is not null) {
                return BadRequest("Leave your team before leaving the event.");
            }
            
            member.TeamId = null;
            member.Team = null;
        }

        await context.SaveChangesAsync();
        
        return Ok();
    }
}