using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.GuildService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Discord; 

[Route("[controller]")]
[ApiController]
[ServiceFilter(typeof(DiscordAuthFilter))]
public class UserController : ControllerBase {
    
    private readonly IDiscordService _discordService;
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IGuildService _guildService;

    public UserController(DataContext context, IMapper mapper, IDiscordService discordService, IGuildService guildService) {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
        _guildService = guildService;
    }
    
    // GET <GuildController>/Guilds
    [HttpGet("Guilds")]
    public async Task<ActionResult<IEnumerable<UserGuildDto>>> Get() {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        return await _discordService.GetUsersGuilds(account.Id, token);
    }
    
    // GET <GuildController>/Guild/{guildId}
    [HttpGet("Guild/{guildId}")]
    public async Task<ActionResult<AuthorizedGuildDto>> GetGuild(ulong guildId) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());
        
        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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
    
    [HttpPut("Guild/{guildId}/Invite")]
    [RequestSizeLimit(512)]
    public async Task<ActionResult> PutGuildInvite(ulong guildId, [FromBody] string inviteCode) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        // Check that the invite code is valid a-Z0-9
        if (!inviteCode.All(char.IsLetterOrDigit) || inviteCode.Length > 16) {
            return BadRequest("Invalid invite code.");
        }
        
        guild.InviteCode = inviteCode;
        
        _context.Guilds.Update(guild);
        await _context.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPatch("Guild/{guildId}/Jacob")]
    public async Task<ActionResult> UpdateGuildJacobFeature(ulong guildId, [FromBody] GuildJacobLeaderboardFeature settings) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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
    
    [HttpPost("Guild/{guildId}/Jacob/{lbId}/Send")]
    public async Task<ActionResult> SendGuildLeaderboard(ulong guildId, string lbId) 
    {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
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

        var channelId = existing.ChannelId;
        if (channelId is null) {
            return BadRequest("Leaderboard channel not set.");
        }

        return await _guildService.SendLeaderboardPanel(guildId, channelId, account.Id, lbId);
    }
    
    [HttpPut("Guild/{guildId}/ContestPings")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> PutGuildContestPings(ulong guildId, [FromBody] ContestPingsFeatureDto feature) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            if (guild?.Features.ContestPings?.Enabled is true) {
                guild.Features.ContestPings.Enabled = false;
                guild.Features.ContestPings.DisabledReason = "Guild no longer found.";
            }
            
            return NotFound("Guild not found.");
        }

        if (!guild.Features.ContestPingsEnabled) {
            return Unauthorized("Contest Pings feature is not enabled for this guild.");
        }

        var pings = guild.Features.ContestPings ?? new ContestPingsFeature();

        pings.Enabled = feature.Enabled;
        pings.ChannelId = feature.ChannelId;
        pings.DelaySeconds = feature.DelaySeconds;
        pings.AlwaysPingRole = feature.AlwaysPingRole;
        pings.CropPingRoles = feature.CropPingRoles;

        if (pings is { Enabled: true, DisabledReason: not null }) {
            pings.DisabledReason = null;
        } 
        
        guild.Features.ContestPings = pings;
        _context.Entry(guild).Property(g => g.Features).IsModified = true;

        await _context.SaveChangesAsync();
        return Accepted();
    }
    
    [HttpDelete("Guild/{guildId}/ContestPings")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> DeleteGuildContestPings(ulong guildId, string reason) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string token) {
            return Unauthorized("Account not found.");
        }

        var guilds = await _discordService.GetUsersGuilds(account.Id, token);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!_guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        await _discordService.GetGuild(guildId);
        var guild = await _context.Guilds.FindAsync(guildId);

        if (guild is null) {
            if (guild?.Features.ContestPings?.Enabled is true) {
                guild.Features.ContestPings.Enabled = false;
                guild.Features.ContestPings.DisabledReason = "Guild no longer found.";
            }
            
            return NotFound("Guild not found.");
        }

        if (!guild.Features.ContestPingsEnabled) {
            return BadRequest("Contest Pings feature is already disabled for this guild.");
        }

        var pings = guild.Features.ContestPings ?? new ContestPingsFeature();

        pings.Enabled = false;
        pings.DisabledReason = reason;
        
        guild.Features.ContestPings = pings;
        _context.Entry(guild).Property(g => g.Features).IsModified = true;

        await _context.SaveChangesAsync();
        return Ok();
    }
    
    [HttpPatch("Badges/{playerUuid}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> UpdateBadgeSettings(string playerUuid, [FromBody] List<EditUserBadgeDto> badges) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string) {
            return Unauthorized("Account not found.");
        }

        var userBadges = await _context.UserBadges
            .Include(a => a.MinecraftAccount)
            .Where(a => a.MinecraftAccountId == playerUuid && a.MinecraftAccount.AccountId == account.Id)
            .ToListAsync();
        
        if (userBadges is { Count: 0 }) {
            return NotFound("Account not found.");
        }

        foreach (var badge in badges) {
            var existing = userBadges.FirstOrDefault(b => b.BadgeId == badge.BadgeId);
            
            if (existing is null) {
                return BadRequest($"Badge {badge.BadgeId} not found on user account.");
            }
            
            existing.Visible = badge.Visible ?? existing.Visible;
            existing.Order = badge.Order ?? existing.Order;
        }
        
        await _context.SaveChangesAsync();
        return Ok();
    }
}