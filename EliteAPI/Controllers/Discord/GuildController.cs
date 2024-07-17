using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Controllers.Discord; 

[ApiController, ApiVersion(1.0)]
[Route("[controller]/{guildId}")]
[Route("/v{version:apiVersion}/[controller]/{guildId}")]
public class GuildController(
    DataContext context, 
    IMapper mapper,
    IConnectionMultiplexer redis,
    IDiscordService discordService)
    : Controller 
{
    /// <summary>
    /// Get list of public guilds
    /// </summary>
    /// <returns></returns>
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GuildDetailsDto>>> GetGuilds() {
        var guilds = await context.Guilds
            .Where(g => g.InviteCode != null && g.IsPublic)
            .OrderByDescending(g => g.MemberCount)
            .Select(g => new GuildDetailsDto {
                Id = g.Id.ToString(), 
                Name = g.Name,
                InviteCode = g.InviteCode,
                Banner = g.Banner,
                Icon = g.Icon,
                MemberCount = g.MemberCount
            })
            .ToListAsync();

        return Ok(guilds);
    }
    
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    
    /// <summary>
    /// Get guild by ID
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <returns></returns>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<PublicGuildDto>> GetGuildById(ulong guildId) {
        if (guildId <= 0) return BadRequest("Invalid guild ID.");

        var db = redis.GetDatabase();
        var key = $"guild:dto:{guildId}";
        var cached = await db.StringGetAsync(key);
        
        if (cached is { IsNullOrEmpty: false, HasValue: true }) {
            var cachedGuild = JsonSerializer.Deserialize<PublicGuildDto>(cached!, JsonOptions);
            return Ok(cachedGuild);
        }

        var guild = await discordService.GetGuild(guildId);
        if (guild is null) return NotFound("Guild not found.");
        if (!guild.IsPublic) return NotFound("Guild is not public.");
        
        var mapped = mapper.Map<PublicGuildDto>(guild);
        await db.StringSetAsync(key, JsonSerializer.Serialize(mapped, JsonOptions), TimeSpan.FromMinutes(2));
        
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get guild events
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <returns></returns>
    [HttpGet("Events")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<List<EventDetailsDto>>> GetGuildEvents(ulong guildId) {
        if (guildId <= 0) return BadRequest("Invalid guild ID.");
        
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) return NotFound("Guild not found.");
        
        var db = redis.GetDatabase();
        var key = $"guild:events:{guildId}";
        var cached = await db.StringGetAsync(key);

        if (cached is { IsNullOrEmpty: false, HasValue: true }) {
            var cachedEvents = JsonSerializer.Deserialize<List<EventDetailsDto>>(cached!, JsonOptions);
            return Ok(cachedEvents);
        }
        
        var events = await context.Events
            .Where(e => e.GuildId == guild.Id)
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .ToListAsync();

        var mapped = mapper.Map<List<EventDetailsDto>>(events) ?? [];
        await db.StringSetAsync(key, JsonSerializer.Serialize(mapped, JsonOptions), TimeSpan.FromMinutes(2));

        return Ok(mapped);
    }
    
    /// <summary>
    /// Enable the guild's Jacob Leaderboard feature
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <param name="max">Max amount of Jacob Leaderboards</param>
    /// <param name="enable">Enable or disable feature</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPost("jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> ToggleGuildJacobFeature(ulong guildId, [FromQuery] int max = 1, [FromQuery] bool enable = true) {
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) {
            return NotFound("Guild not found.");
        }

        guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
        guild.Features.JacobLeaderboard.MaxLeaderboards = max;
        guild.Features.JacobLeaderboardEnabled = enable;

        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok();
    }

    /// <summary>
    /// Enable the guild's Event feature
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <param name="max">Max amount of Events</param>
    /// <param name="enable">Enable or disable feature</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPost("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> ToggleGuildEventsFeature(ulong guildId, [FromQuery] int max = 1, [FromQuery] bool enable = true) {
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        guild.Features.EventSettings ??= new GuildEventSettings();
        guild.Features.EventSettings.MaxMonthlyEvents = max;
        guild.Features.EventsEnabled = enable;

        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Set the guild's public visibility
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="enable"></param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPost("public")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> SetGuildVisibility(ulong guildId, [FromQuery] bool enable = true) {
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        guild.IsPublic = enable;
        await context.SaveChangesAsync();

        return Ok();
    }
    
    /// <summary>
    /// Set the guild's locked status
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <param name="locked">If server subscriptions should overwrite feature values.</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPost("lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> LockGuildFeatures(ulong guildId, [FromQuery] bool locked = true) {
        var guild = await discordService.GetGuild(guildId);
        if (guild is null) {
            return NotFound("Guild not found.");
        }
        
        guild.Features.Locked = locked;

        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok();
    }
}