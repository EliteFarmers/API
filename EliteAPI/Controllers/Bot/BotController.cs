﻿using Asp.Versioning;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Bot; 

[ServiceFilter(typeof(DiscordBotOnlyFilter))]
[ApiController, ApiVersion(1.0)]
[Route("[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public class BotController(
    DataContext context, 
    IMapper mapper,
    IDiscordService discordService,
    IGuildService guildService,
    IAccountService accountService, 
    ILogger<BotController> logger,
    IMonetizationService monetizationService,
    IBadgeService badgeService)
    : ControllerBase
{
    /// <summary>
    /// Get a guild by ID
    /// </summary>
    /// <param name="guildId">Discord server (guild) ID</param>
    /// <returns></returns>
    [HttpGet("{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<PrivateGuildDto>> Get(ulong guildId)
    {
        var guild = await context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");
        
        return Ok(mapper.Map<PrivateGuildDto>(guild));
    }
    
    /// <summary>
    /// Get Jacob Leaderboard feature for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    [HttpGet("{guildId}/jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> GetJacobFeature(ulong guildId)
    {
        var guild = await context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");
        
        if (!guild.Features.JacobLeaderboardEnabled) {
            return BadRequest("Jacob leaderboards are not enabled for this guild");
        }

        if (guild.Features.JacobLeaderboard is not null) return Ok(guild.Features.JacobLeaderboard);
        
        guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
        
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();

        return Ok(guild.Features.JacobLeaderboard);
    }
    
    /// <summary>
    /// Update Jacob Leaderboard feature for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    // PUT <GuildController>/12793764936498429/jacob
    [HttpPut("{guildId}/jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> PutJacobFeature(ulong guildId, [FromBody] GuildJacobLeaderboardFeature data)
    {
        var guild = await context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");

        if (!guild.Features.JacobLeaderboardEnabled) {
            return BadRequest("Jacob leaderboards are not enabled for this guild");
        }

        guild.Features.JacobLeaderboard = data;
        
        context.Guilds.Update(guild);
        await context.SaveChangesAsync();
        
        return Accepted();
    }

    /// <summary>
    /// Get list of guilds with Contest Pings enabled
    /// </summary>
    /// <returns></returns>
    // GET <BotController>/ContestPings
    [HttpGet("ContestPings")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ContestPingsFeatureDto>))]
    public async Task<ActionResult<List<ContestPingsFeatureDto>>> GetContestPingServers() {
        var guilds = await context.Guilds
            .Where(g => g.Features.ContestPingsEnabled == true
                        && g.Features.ContestPings != null
                        && g.Features.ContestPings.Enabled)
            .ToListAsync();

        return Ok(guilds
            .Where(g => !string.IsNullOrWhiteSpace(g.Features.ContestPings!.ChannelId))
            .Select(g => new ContestPingsFeatureDto {
                GuildId = g.Id.ToString(),
                ChannelId = g.Features.ContestPings?.ChannelId ?? string.Empty,
                AlwaysPingRole = g.Features.ContestPings?.AlwaysPingRole ?? string.Empty,
                CropPingRoles = g.Features.ContestPings?.CropPingRoles ?? new CropSettings<string>(),
                DelaySeconds = g.Features.ContestPings?.DelaySeconds ?? 0,
                DisabledReason = g.Features.ContestPings?.DisabledReason ?? string.Empty,
                Enabled = g.Features.ContestPings?.Enabled ?? false
            }));
    }
    
    /// <summary>
    /// Delete Contest Pings feature for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    // DELETE <BotController>/ContestPings/12793764936498429
    [HttpDelete("ContestPings/{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> DeleteGuildContestPings(long guildId, string reason) {
        var guild = await context.Guilds.FindAsync((ulong) guildId);

        if (guild is null) {
            return NotFound("Guild Not Found");
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
    /// Update user's Discord account
    /// </summary>
    /// <param name="incoming"></param>
    /// <returns></returns>
    // Patch <GuildController>/account
    [HttpPatch("account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthorizedAccountDto>> PatchAccount([FromBody] IncomingAccountDto incoming) {
        var exising = await accountService.GetAccount(incoming.Id);
        
        var account = exising ?? new EliteAccount {
            Id = incoming.Id,
            Username = incoming.Username,
            DisplayName = incoming.DisplayName ?? incoming.Username,
        };

        account.Avatar = incoming.Avatar ?? account.Avatar;
        account.DisplayName = incoming.DisplayName ?? account.DisplayName;
        account.Locale = incoming.Locale ?? account.Locale;
        
        account.Discriminator = incoming.Discriminator;

        if (exising is null) {
            try {
                await context.Accounts.AddAsync(account);
                await context.SaveChangesAsync();
            } catch (Exception e) {
                logger.LogWarning("Failed to add account to database: {Error}", e);
            }
        } else {
            context.Accounts.Update(account);
        }
        
        return Ok(mapper.Map<AuthorizedAccountDto>(account));
    }
    
    /// <summary>
    /// Request Discord Guild Update
    /// </summary>
    /// <returns></returns>
    [HttpPost("guild/{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshGuild(ulong guildId) {
        await discordService.RefreshDiscordGuild(guildId);
        return Ok();
    }

    /// <summary>
    /// Update Discord Guild
    /// </summary>
    /// <returns></returns>
    [HttpPatch("guild/{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGuild(ulong guildId, [FromBody] IncomingGuildDto incoming) {
        await guildService.UpdateGuildData(guildId, incoming);
        return Ok();
    }
    
    /// <summary>
    /// Update Discord Guild
    /// </summary>
    /// <returns></returns>
    [HttpPost("guild/{guildId}/channels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGuildChannel(ulong guildId, [FromBody] IncomingGuildChannelDto incoming) {
        await guildService.UpdateGuildChannelData(guildId, incoming);
        return Ok();
    }
    
    /// <summary>
    /// Update Discord Guild
    /// </summary>
    /// <returns></returns>
    [HttpPost("guild/{guildId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGuildRole(ulong guildId, [FromBody] IncomingGuildRoleDto incoming) {
        await guildService.UpdateGuildRoleData(guildId, incoming);
        return Ok();
    }
    
    /// <summary>
    /// Update Guild Memeber Roles
    /// </summary>
    /// <returns></returns>
    [HttpPut("guild/{guildId}/members/{userId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> UpdateGuildMemberRoles(ulong guildId, string userId, [FromBody] List<string> incoming) {
        var member = await context.GuildMembers
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.AccountId == userId);
        
        if (member is null) {
            return NotFound("Member not found");
        }

        try {
            member.Roles = incoming.Select(ulong.Parse).ToList();
            member.LastUpdated = DateTimeOffset.UtcNow;
        } catch (Exception e) {
            logger.LogWarning("Failed to update member roles: {Error}", e);
            return BadRequest("Failed to update member roles");
        }
        
        context.GuildMembers.Update(member);
        await context.SaveChangesAsync();
        return Ok();
    }
    
    /// <summary>
    /// Add a badge to a player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="badgeId"></param>
    /// <returns></returns>
    // POST <BotController>/Badges/7da0c47581dc42b4962118f8049147b7
    [HttpPost("Badges/{playerUuid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> AddPlayerBadge(string playerUuid, int badgeId) {
        return await badgeService.AddBadgeToUser(playerUuid, badgeId);
    }
    
    /// <summary>
    /// Remove a badge from a player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="badgeId"></param>
    /// <returns></returns>
    [HttpDelete("Badges/{playerUuid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> RemovePlayerBadge(string playerUuid, int badgeId) {
        return await badgeService.RemoveBadgeFromUser(playerUuid, badgeId);
    }
    
    /// <summary>
    /// Link a Minecraft account to a Discord account
    /// </summary>
    /// <param name="discordId"></param>
    /// <param name="playerIgnOrUuid"></param>
    /// <returns></returns>
    // Post <GuildController>/account/12793764936498429/Ke5o
    [HttpPost("account/{discordId:long:min(0)}/{playerIgnOrUuid}")]
    public async Task<ActionResult> LinkAccount(long discordId, string playerIgnOrUuid) {
        return await accountService.LinkAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    /// <summary>
    /// Unlink a Minecraft account from a Discord account
    /// </summary>
    /// <param name="discordId"></param>
    /// <param name="playerIgnOrUuid"></param>
    /// <returns></returns>
    // Delete <GuildController>/account/12793764936498429/Ke5o
    [HttpDelete("account/{discordId:long:min(0)}/{playerIgnOrUuid}")]
    public async Task<ActionResult> UnlinkAccount(long discordId, string playerIgnOrUuid) {
        return await accountService.UnlinkAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    /// <summary>
    /// Set a Minecraft account as primary
    /// </summary>
    /// <param name="discordId"></param>
    /// <param name="playerIgnOrUuid"></param>
    /// <returns></returns>
    // Post <GuildController>/account/12793764936498429/Ke5o/primary
    [HttpPost("account/{discordId:long:min(0)}/{playerIgnOrUuid}/primary")]
    public async Task<ActionResult> MakePrimaryAccount(long discordId, string playerIgnOrUuid) {
        return await accountService.MakePrimaryAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    /// <summary>
    /// Refresh user purchases
    /// </summary>
    /// <param name="discordId"></param>
    /// <returns></returns>
    [HttpPost("account/{discordId:long:min(0)}/purchases")]
    public async Task<ActionResult> RefreshUserEntitlements(ulong discordId) {
        await monetizationService.FetchUserEntitlementsAsync(discordId);
        return Ok();
    }
}