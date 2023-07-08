using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.LeaderboardService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly DataContext _context;
    private readonly ILeaderboardService _leaderboardService;
    private readonly ConfigLeaderboardSettings _settings;

    public LeaderboardController(DataContext dataContext, ILeaderboardService leaderboardService, IOptions<ConfigLeaderboardSettings> lbSettings)
    {
        _context = dataContext;
        _leaderboardService = leaderboardService;
        _settings = lbSettings.Value;
    }

    // GET: api/<LeaderboardController>/id
    [HttpGet("{id}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<LeaderboardDto>> Get(string id, [FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (offset < 0 || limit <= 0) return BadRequest("Offset and limit must be positive integers");

        if (!_settings.Leaderboards.TryGetValue(id, out var lb)) {
            return BadRequest("Leaderboard not found");
        }

        var entries = await _leaderboardService.GetLeaderboardSlice(id, offset, limit);

        var leaderboard = new LeaderboardDto {
            Id = id,
            Title = lb.Title,
            Limit = limit,
            Offset = offset,
            Entries = entries
        };
        
        return Ok(leaderboard);
    }

    // GET api/<LeaderboardController>/skill/farming
    [HttpGet("skill/{skillName}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
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
        
        var entries = await _leaderboardService.GetSkillLeaderboardSlice(skill, offset, limit);

        return Ok(new LeaderboardDto {
            Id = lb.Id,
            Title = lb.Title,
            Offset = offset,
            Limit = limit,
            Entries = entries
        });
    }
    
    // GET api/<LeaderboardController>/collection/wheat
    [HttpGet("collection/{collection}/")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<LeaderboardDto>> GetCollectionLb(string collection, [FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (offset < 0 || limit <= 0) return BadRequest("Offset and limit must be positive integers");
        
        if (!_settings.CollectionLeaderboards.TryGetValue(collection, out var lb))
        {
            return BadRequest("Invalid collection.");
        }
        
        var entries = await _leaderboardService.GetCollectionLeaderboardSlice(collection, offset, limit);

        return Ok(new LeaderboardDto {
            Id = lb.Id,
            Title = lb.Title,
            Offset = offset,
            Limit = limit,
            Entries = entries
        });
    }
    
    // GET api/<LeaderboardController>/ranks/[player]/[profile]
    [HttpGet("ranks/{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<LeaderboardPositionsDto>> GetLeaderboardRanks(string playerUuid, string profileUuid) {
        var memberId = await _context.ProfileMembers
            .Where(p => p.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (memberId == Guid.Empty) return BadRequest("Invalid player or profile UUID.");
        
        var positions = await _leaderboardService.GetLeaderboardPositions(memberId.ToString());
        
        return Ok(positions);
    }
    
    // GET api/<LeaderboardController>/rank/[lbId]/[player]/[profile]
    [HttpGet("rank/{leaderboardId}/{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 5, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<LeaderboardPositionDto>> GetLeaderboardRank(string leaderboardId, string playerUuid, string profileUuid, [FromQuery] bool includeUpcoming = false) {
        if (!_leaderboardService.TryGetLeaderboardSettings(leaderboardId, out var lb) || lb is null) {
            return BadRequest("Invalid leaderboard ID.");
        }

        var memberId = await _context.ProfileMembers
            .Where(p => p.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (memberId == Guid.Empty) return BadRequest("Invalid player or profile UUID.");
        
        var position = await _leaderboardService.GetLeaderboardPosition(leaderboardId, memberId.ToString());
        List<LeaderboardEntry>? upcomingPlayers = null;

        if (includeUpcoming && position == -1) {
            upcomingPlayers = await _leaderboardService.GetLeaderboardSlice(leaderboardId, lb.Limit - 1, 1);
        } else if (includeUpcoming && position > 1) {
            upcomingPlayers = await _leaderboardService.GetLeaderboardSlice(leaderboardId, Math.Max(position - 6, 0),
                Math.Min(position - 1, 5));
        }
        
        var result = new LeaderboardPositionDto {
            Rank = position,
            UpcomingPlayers = upcomingPlayers
        };
        
        return Ok(result);
    }
}
