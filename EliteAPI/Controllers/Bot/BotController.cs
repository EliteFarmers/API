using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Bot; 

[Route("[controller]")]
[ServiceFilter(typeof(DiscordBotOnlyFilter))]
[ApiController]
public class BotController(DataContext context, IMapper mapper, IDiscordService discordService,
        IAccountService accountService, ILogger<BotController> logger)
    : ControllerBase
{
    // GET <BotController>/12793764936498429
    [HttpGet("{guildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<GuildDto>> Get(ulong guildId)
    {
        await discordService.RefreshBotGuilds();
        var guild = await context.Guilds.FindAsync(guildId);
        if (guild is null) return NotFound("Guild not found");
        
        return Ok(mapper.Map<GuildDto>(guild));
    }
    
    // GET <BotController>/12793764936498429/jacob
    [HttpGet("{guildId}/jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<GuildJacobLeaderboardFeature>> GetJacobFeature(ulong guildId)
    {
        await discordService.RefreshBotGuilds();
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
    
    // PUT <GuildController>/12793764936498429/jacob
    [HttpPut("{guildId}/jacob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> PutJacobFeature(ulong guildId, [FromBody] GuildJacobLeaderboardFeature data)
    {
        await discordService.RefreshBotGuilds();
        
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
    
    // GET <BotController>/ContestPings
    [HttpGet("ContestPings")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ContestPingsFeatureDto>))]
    public async Task<ActionResult<List<ContestPingsFeatureDto>>> GetContestPingServers()
    {
        var guilds = await context.Guilds
            .Where(g => g.Features.ContestPingsEnabled == true
                        && g.Features.ContestPings != null 
                        && g.Features.ContestPings.Enabled)
            .ToListAsync();
        
        return Ok(guilds.Select(g => new ContestPingsFeatureDto {
            GuildId = g.Id.ToString(),
            ChannelId = g.Features.ContestPings?.ChannelId ?? "",
            AlwaysPingRole = g.Features.ContestPings?.AlwaysPingRole ?? "",
            CropPingRoles = g.Features.ContestPings?.CropPingRoles ?? new CropSettings<string>(),
            DelaySeconds = g.Features.ContestPings?.DelaySeconds ?? 0,
            DisabledReason = g.Features.ContestPings?.DisabledReason ?? "",
            Enabled = g.Features.ContestPings?.Enabled ?? false
        }));
    }
    
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
    
    // POST <BotController>/Badges/7da0c47581dc42b4962118f8049147b7
    [HttpPost("Badges/{playerUuid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> AddPlayerBadge(string playerUuid, int badgeId) {
        var member = await context.MinecraftAccounts
            .Include(m => m.Badges)
            .FirstOrDefaultAsync(a => a.Id == playerUuid);
        
        if (member is null) {
            return NotFound("User not found.");
        }
        
        var badge = await context.Badges
            .FirstOrDefaultAsync(b => b.Id == badgeId);
        
        if (badge is null) {
            return NotFound("Badge not found.");
        }
        
        if (member.Badges.Any(b => b.BadgeId == badgeId)) {
            return BadRequest("User already has this badge.");
        }
        
        var userBadge = new UserBadge {
            BadgeId = badge.Id,
            MinecraftAccountId = member.Id,
            Visible = true
        };
        
        context.UserBadges.Add(userBadge);
        
        // Add badge to user's relationship
        member.Badges.Add(userBadge);
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpDelete("Badges/{playerUuid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> RemovePlayerBadge(string playerUuid, int badgeId) {
        var userBadge = await context.UserBadges
            .FirstOrDefaultAsync(a => a.MinecraftAccountId == playerUuid && a.BadgeId == badgeId);
        
        if (userBadge is null) {
            return NotFound("User badge not found.");
        }
        
        context.UserBadges.Remove(userBadge);
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    // Post <GuildController>/account/12793764936498429/Ke5o
    [HttpPost("account/{discordId:long:min(0)}/{playerIgnOrUuid}")]
    public async Task<ActionResult> LinkAccount(long discordId, string playerIgnOrUuid) {
        return await accountService.LinkAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    // Delete <GuildController>/account/12793764936498429/Ke5o
    [HttpDelete("account/{discordId:long:min(0)}/{playerIgnOrUuid}")]
    public async Task<ActionResult> UnlinkAccount(long discordId, string playerIgnOrUuid) {
        return await accountService.UnlinkAccount((ulong) discordId, playerIgnOrUuid);
    }
    
    // Post <GuildController>/account/12793764936498429/Ke5o/primary
    [HttpPost("account/{discordId:long:min(0)}/{playerIgnOrUuid}/primary")]
    public async Task<ActionResult> MakePrimaryAccount(long discordId, string playerIgnOrUuid) {
        return await accountService.MakePrimaryAccount((ulong) discordId, playerIgnOrUuid);
    }
}