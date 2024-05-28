using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.LeaderboardService;
using EliteAPI.Services.ProfileService;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class LeaderboardController(
    DataContext dataContext,
    ILeaderboardService leaderboardService,
    IOptions<ConfigLeaderboardSettings> lbSettings,
    IMapper mapper,
    IOptions<ConfigCooldownSettings> coolDowns,
    IProfileService profileService)
    : ControllerBase 
{
    private readonly ConfigLeaderboardSettings _settings = lbSettings.Value;
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
    /// <summary>
    /// Get a list of leaderboards
    /// </summary>
    /// <returns></returns>
    [HttpGet("/[controller]s")]
    [ResponseCache(Duration = 60 * 60 * 5, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ConfigLeaderboardSettings> GetLeaderboardSettings() {
        return Ok(_settings);
    }
    
    // GET: <LeaderboardController>/id
    /// <summary>
    /// Get a leaderboard by ID
    /// </summary>
    /// <param name="id">Any leaderboard ID</param>
    /// <param name="offset">Starting offset for returned entries</param>
    /// <param name="limit">Maximum amount of returned entries</param>
    /// <returns>A leaderboard object with a list of entries</returns>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<LeaderboardDto>> Get(string id, [FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (offset < 0 || limit <= 0) return BadRequest("Offset and limit must be positive integers");

        if (!leaderboardService.TryGetLeaderboardSettings(id, out var lb) || lb is null) {
            return BadRequest("Leaderboard not found");
        }

        var entries = await leaderboardService.GetLeaderboardSlice(id, offset, limit);

        var leaderboard = new LeaderboardDto {
            Id = id,
            Title = lb.Title,
            Limit = limit,
            Offset = offset,
            MaxEntries = lb.Limit,
            Entries = mapper.Map<List<LeaderboardEntryDto>>(entries)
        };
        
        return Ok(leaderboard);
    }

    // GET <LeaderboardController>/skill/farming
    /// <summary>
    /// Get a skill leaderboard by name
    /// </summary>
    /// <param name="skillName">A skill name</param>
    /// <param name="offset">Starting offset for returned entries</param>
    /// <param name="limit">Maximum amount of returned entries</param>
    /// <returns>A leaderboard object with a list of entries</returns>
    [HttpGet("skill/{skillName}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<LeaderboardDto>> GetSkillLb(string skillName, [FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (offset < 0 || limit <= 0) return BadRequest("Offset and limit must be positive integers");
        
        var skill = skillName.ToLower() switch
        {
            "farming" => SkillName.Farming,
            "mining" => SkillName.Mining, 
            "combat" => SkillName.Combat,
            "foraging" => SkillName.Foraging,
            "fishing" => SkillName.Fishing,
            "enchanting" => SkillName.Enchanting,
            "alchemy" => SkillName.Alchemy,
            "carpentry" => SkillName.Carpentry,
            "runecrafting" => SkillName.Runecrafting,
            "taming" => SkillName.Taming,
            "social" => SkillName.Social,
            _ => null
        };

        if (skill is null || !_settings.SkillLeaderboards.TryGetValue(skill, out var lb))
        {
            return BadRequest("Invalid skill.");
        }
        
        var entries = await leaderboardService.GetLeaderboardSlice(skill, offset, limit);

        return Ok(new LeaderboardDto {
            Id = lb.Id,
            Title = lb.Title,
            Offset = offset,
            Limit = limit,
            MaxEntries = lb.Limit,
            Entries = mapper.Map<List<LeaderboardEntryDto>>(entries)
        });
    }
    
    // GET <LeaderboardController>/collection/wheat
    /// <summary>
    /// Get a crop collection leaderboard by name
    /// </summary>
    /// <param name="collection">A crop collection name</param>
    /// <param name="offset">Starting offset for returned entries</param>
    /// <param name="limit">Maximum amount of returned entries</param>
    /// <returns>A leaderboard object with a list of entries</returns>
    [HttpGet("collection/{collection}/")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<LeaderboardDto>> GetCollectionLb(string collection, [FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (offset < 0 || limit <= 0) return BadRequest("Offset and limit must be positive integers");
        
        if (!_settings.CollectionLeaderboards.TryGetValue(collection, out var lb))
        {
            return BadRequest("Invalid collection.");
        }
        
        var entries = await leaderboardService.GetLeaderboardSlice(collection, offset, limit);

        return Ok(new LeaderboardDto {
            Id = lb.Id,
            Title = lb.Title,
            Offset = offset,
            Limit = limit,
            MaxEntries = lb.Limit,
            Entries = mapper.Map<List<LeaderboardEntryDto>>(entries)
        });
    }
    
    // GET <LeaderboardController>/ranks/[player]/[profile]
    /// <summary>
    /// Get a player's leaderboard ranks
    /// </summary>
    /// <param name="playerUuid">Player Uuid (no hyphens)</param>
    /// <param name="profileUuid">Profile Uuid (no hyphens)</param>
    /// <returns></returns>
    [HttpGet("ranks/{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<LeaderboardPositionsDto>> GetLeaderboardRanks(string playerUuid, string profileUuid) {
        var memberId = await dataContext.ProfileMembers.AsNoTracking()
            .Where(p => p.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (memberId == Guid.Empty) return BadRequest("Invalid player or profile UUID.");
        
        var positions = await leaderboardService.GetLeaderboardPositions(memberId.ToString());
        
        return Ok(positions);
    }
    
    // GET <LeaderboardController>/rank/[lbId]/[player]/[profile]
    /// <summary>
    /// Get a player's rank in a leaderboard
    /// </summary>
    /// <param name="leaderboardId">Any leaderboard ID</param>
    /// <param name="playerUuid">Player Uuid (no hyphens)</param>
    /// <param name="profileUuid">Profile Uuid (no hyphens)</param>
    /// <param name="includeUpcoming">Include upcoming players</param>
    /// <param name="atRank">Starting rank for upcoming players</param>
    /// <returns></returns>
    [HttpGet("rank/{leaderboardId}/{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<LeaderboardPositionDto>> GetLeaderboardRank(string leaderboardId, string playerUuid, string profileUuid, [FromQuery] bool includeUpcoming = false, [FromQuery] int atRank = -1) {
        if (!leaderboardService.TryGetLeaderboardSettings(leaderboardId, out var lb) || lb is null) {
            return BadRequest("Invalid leaderboard ID.");
        }

        var member = await dataContext.ProfileMembers
            .Where(p => p.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .Select(p => new { p.Id, p.LastUpdated, p.PlayerUuid })
            .FirstOrDefaultAsync();

        if (member is null || member.Id == Guid.Empty) return BadRequest("Invalid player or profile UUID.");

        // Update the profile if it's older than the cooldown
        if (member.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown)) {
            await profileService.GetSelectedProfileMember(member.PlayerUuid);
        }
        
        var (position, score) = await leaderboardService.GetLeaderboardPositionAndScore(leaderboardId, member.Id.ToString());
        List<LeaderboardEntry>? upcomingPlayers = null;
        
        var rank = atRank == -1 ? position : Math.Min(Math.Max(1, atRank), lb.Limit + 1);
        rank = position != -1 ? Math.Min(position, rank) : rank;

        if (includeUpcoming && rank == -1) {
            upcomingPlayers = await leaderboardService.GetLeaderboardSlice(leaderboardId, lb.Limit - 1, 1);
        } else if (includeUpcoming && rank > 1) {
            upcomingPlayers = await leaderboardService.GetLeaderboardSlice(leaderboardId, Math.Max(rank - 6, 0),
                Math.Min(rank - 1, 5));
        }

        // Reverse the list so that upcoming players are in ascending order
        upcomingPlayers?.Reverse();
        var upcoming = mapper.Map<List<LeaderboardEntryDto>>(upcomingPlayers);

        var result = new LeaderboardPositionDto {
            Rank = position,
            Amount = score,
            UpcomingRank = rank == -1 ? lb.Limit : rank - 1,
            UpcomingPlayers = upcoming
        };
        
        return Ok(result);
    }
}
