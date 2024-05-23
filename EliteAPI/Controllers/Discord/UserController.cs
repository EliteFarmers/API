using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.GuildService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Discord; 

[Route("[controller]")]
[ApiController]
[Authorize]
public class UserController(
    DataContext context,
    IMapper mapper,
    IDiscordService discordService,
    IGuildService guildService,
    UserManager<ApiUser> userManager)
    : ControllerBase 
{
    /// <summary>
    /// Get the user's guilds
    /// </summary>
    /// <returns></returns>
    [HttpGet("Guilds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<UserGuildDto>>> Get() {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }

        return await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
    }

    /// <summary>
    /// Get the user's guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    [HttpGet("Guild/{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult<AuthorizedGuildDto>> GetGuild(ulong guildId) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains(ApiUserRoles.Admin);

        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (isAdmin) {
            userGuild = new UserGuildDto {
                Id = guildId.ToString(),
                Permissions = "8",
                Name = string.Empty
            };
        }

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (guild.Features is { JacobLeaderboardEnabled: true, JacobLeaderboard: null }) {
            guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
            
            context.Guilds.Update(guild);
            await context.SaveChangesAsync();
        }

        if (guild.Features is { VerifiedRoleEnabled: true, VerifiedRole: null }) {
            guild.Features.VerifiedRole = new VerifiedRoleFeature();
            
            context.Guilds.Update(guild);
            await context.SaveChangesAsync();
        }
        
        return Ok(new AuthorizedGuildDto {
            Id = guildId.ToString(),
            Permissions = userGuild.Permissions,
            DiscordGuild = fullGuild,
            Guild = mapper.Map<GuildDto>(guild)
        });
    }
    
    /// <summary>
    /// Set the guild's Discord invite
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="inviteCode"></param>
    /// <returns></returns>
    [HttpPut("Guild/{guildId}/Invite")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> PutGuildInvite(ulong guildId, [FromBody] string inviteCode) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        // Check that the invite code is valid a-Z0-9
        if (!inviteCode.All(char.IsLetterOrDigit) || inviteCode.Length > 16) {
            return BadRequest("Invalid invite code.");
        }
        
        guild.InviteCode = inviteCode;
        
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();

        return Ok();
    }
    
    /// <summary>
    /// Update the guild's Jacob Leaderboard feature
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    [HttpPatch("Guild/{guildId}/Jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> UpdateGuildJacobFeature(ulong guildId, [FromBody] GuildJacobLeaderboardFeature settings) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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
        
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Get the guild's Jacob Leaderboards
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    [HttpGet("Guild/{guildId}/Jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> UpdateGuildJacobFeature(ulong guildId) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

        if (fullGuild is null || guild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guild.Features.JacobLeaderboardEnabled) {
            return Unauthorized("Jacob Leaderboard feature is not enabled for this guild.");
        }

        if (guild.Features.JacobLeaderboard is not null) return Ok(guild.Features.JacobLeaderboard);
        
        guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
            
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
            
        return Ok(guild.Features.JacobLeaderboard);
    }
    
    /// <summary>
    /// Add a guild Jacob Leaderboard
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="leaderboard"></param>
    /// <returns></returns>
    [HttpPost("Guild/{guildId}/Jacob/Leaderboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> AddGuildLeaderboard(ulong guildId, [FromBody] GuildJacobLeaderboard leaderboard) 
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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
        
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Replace a guild Jacob Leaderboard
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="lbId"></param>
    /// <param name="leaderboard"></param>
    /// <returns></returns>
    [HttpPut("Guild/{guildId}/Jacob/{lbId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> UpdateGuildLeaderboard(ulong guildId, string lbId, [FromBody] GuildJacobLeaderboard leaderboard) 
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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
        
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok(feature);
    }
    
    /// <summary>
    /// Delete a guild Jacob Leaderboard
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="lbId"></param>
    /// <returns></returns>
    [HttpDelete("Guild/{guildId}/Jacob/{lbId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> RemoveGuildLeaderboard(ulong guildId, string lbId) 
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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

        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Send a guild Jacob Leaderboard to Discord
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="lbId"></param>
    /// <returns></returns>
    [HttpPost("Guild/{guildId}/Jacob/{lbId}/Send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> SendGuildLeaderboard(ulong guildId, string lbId) 
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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

        return await guildService.SendLeaderboardPanel(guildId, channelId, user.AccountId.Value, lbId);
    }
    
    /// <summary>
    /// Update the guild's contest ping feature
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="feature"></param>
    /// <returns></returns>
    [HttpPut("Guild/{guildId}/ContestPings")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    public async Task<ActionResult> PutGuildContestPings(ulong guildId, [FromBody] ContestPingsFeatureDto feature) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        var fullGuild = await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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
        context.Entry(guild).Property(g => g.Features).IsModified = true;

        await context.SaveChangesAsync();
        return Accepted();
    }
    
    /// <summary>
    /// Remove the guild's contest ping feature
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    [HttpDelete("Guild/{guildId}/ContestPings")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> DeleteGuildContestPings(ulong guildId, string reason) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (userGuild is null) {
            return NotFound("Guild not found.");
        }
        
        if (!guildService.HasGuildAdminPermissions(userGuild)) {
            return Unauthorized("You do not have permission to access this guild.");
        }
        
        await discordService.GetGuild(guildId);
        var guild = await context.Guilds.FindAsync(guildId);

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
        context.Entry(guild).Property(g => g.Features).IsModified = true;

        await context.SaveChangesAsync();
        return Ok();
    }
    
    /// <summary>
    /// Update a user's badge settings
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="badges"></param>
    /// <returns></returns>
    [HttpPatch("Badges/{playerUuid}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> UpdateBadgeSettings(string playerUuid, [FromBody] List<EditUserBadgeDto> badges) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            return BadRequest("Linked account not found.");
        }
        
        var userBadges = await context.UserBadges
            .Include(a => a.MinecraftAccount)
            .Where(a => a.MinecraftAccountId == playerUuid && a.MinecraftAccount.AccountId == user.AccountId.Value)
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
        
        await context.SaveChangesAsync();
        return Ok();
    }
}