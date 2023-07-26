using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers.Discord; 

[Route("[controller]")]
[ApiController]
[ServiceFilter(typeof(DiscordAuthFilter))]
public class UserController : ControllerBase {
    
    private readonly IDiscordService _discordService;
    private readonly DataContext _context;
    private readonly IMapper _mapper;

    public UserController(DataContext context, IMapper mapper, IDiscordService discordService) {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
    }
    
    // GET <GuildController>/Guilds
    [HttpGet("Guilds")]
    public async Task<ActionResult<IEnumerable<UserGuildDto>>> Get() {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        return await _discordService.GetUsersGuilds(account.Id, token);
    }
    
    // GET <GuildController>/Guild/{guildId}
    [HttpGet("Guild/{guildId}")]
    public async Task<ActionResult<AuthorizedGuildDto>> GetGuild(ulong guildId) {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (guild.Features is { JacobLeaderboardEnabled: true, JacobLeaderboard: null }) {
            guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
            
            _context.Guilds.Update(guild);
            await _context.SaveChangesAsync();
        }

        if (guild.Features is { VerifiedRoleEnabled: true, VerifiedRole: null }) {
            guild.Features.VerifiedRole = new VerifiedRoleFeature();
            
            _context.Guilds.Update(guild);
            await _context.SaveChangesAsync();
        }
        
        return Ok(new AuthorizedGuildDto {
            Id = guildId.ToString(),
            Permissions = userGuild.Permissions,
            DiscordGuild = fullGuild,
            Guild = _mapper.Map<GuildDto>(guild)
        });
    }
    
    [HttpPatch("Guild/{guildId}/Jacob")]
    public async Task<ActionResult> UpdateGuildJacobFeature(ulong guildId, [FromBody] GuildJacobLeaderboardFeature settings) {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
            return Unauthorized("Jacob Leaderboard feature is not enabled for this guild.");
        }
        
        var feature = guild.Features.JacobLeaderboard;

        feature.MaxLeaderboards = settings.MaxLeaderboards;
        feature.BlockedRoles = settings.BlockedRoles;
        feature.BlockedUsers = settings.BlockedUsers;
        feature.RequiredRoles = settings.RequiredRoles;
        feature.ExcludedParticipations = settings.ExcludedParticipations;
        feature.ExcludedTimespans = settings.ExcludedTimespans;
        
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpGet("Guild/{guildId}/Jacob")]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> UpdateGuildJacobFeature(ulong guildId) {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guild.Features.JacobLeaderboardEnabled) {
            return Unauthorized("Jacob Leaderboard feature is not enabled for this guild.");
        }

        if (guild.Features.JacobLeaderboard is not null) return Ok(guild.Features.JacobLeaderboard);
        
        guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
            
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();
            
        return Ok(guild.Features.JacobLeaderboard);
    }
    
    [HttpPost("Guild/{guildId}/Jacob/Leaderboard")]
    public async Task<ActionResult> AddGuildLeaderboard(ulong guildId, [FromBody] GuildJacobLeaderboard leaderboard) 
    {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }

        if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
            return Unauthorized("Jacob Leaderboard feature is not enabled for this guild.");
        }
        
        var feature = guild.Features.JacobLeaderboard;
        
        if (feature.Leaderboards.Count >= feature.MaxLeaderboards) {
            return BadRequest("You have reached the maximum amount of leaderboards.");
        }
        
        if (feature.Leaderboards.Any(l => l.Id.Equals(leaderboard.Id))) {
            return BadRequest("A leaderboard with this id already exists.");
        }
        
        feature.Leaderboards.Add(leaderboard);
        
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpPut("Guild/{guildId}/Jacob/{lbId}")]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> UpdateGuildLeaderboard(ulong guildId, string lbId, [FromBody] GuildJacobLeaderboard leaderboard) 
    {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }

        if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
            return Unauthorized("Jacob Leaderboard feature is not enabled for this guild.");
        }
        
        var feature = guild.Features.JacobLeaderboard;
        var existing = feature.Leaderboards.FirstOrDefault(lb => lb.Id.Equals(lbId));
        
        if (existing is null) {
            return NotFound("Leaderboard not found.");
        }

        feature.Leaderboards.Remove(existing);
        feature.Leaderboards.Add(leaderboard);
        
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();
        
        return Ok(feature);
    }
    
    [HttpDelete("Guild/{guildId}/Jacob/{lbId}")]
    public async Task<ActionResult> RemoveGuildLeaderboard(ulong guildId, string lbId) 
    {
        if (HttpContext.Items["Account"] is not AccountEntity account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }

        if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
            return Unauthorized("Jacob Leaderboard feature is not enabled for this guild.");
        }
        
        var feature = guild.Features.JacobLeaderboard;
        var existing = feature.Leaderboards.FirstOrDefault(lb => lb.Id.Equals(lbId));
        
        if (existing is null) {
            return NotFound("Leaderboard not found.");
        }

        feature.Leaderboards.Remove(existing);

        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
}