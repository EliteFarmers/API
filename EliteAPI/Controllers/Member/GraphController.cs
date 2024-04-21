using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.TimescaleService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Member; 

[Route("[controller]/{playerUuid:length(32)}")]
[ApiController]
public class GraphController : ControllerBase {
    private readonly ITimescaleService _timescaleService;
    private readonly DataContext _context;

    public GraphController(DataContext context, ITimescaleService timescaleService) {
        _timescaleService = timescaleService;
        _context = context;
    }
    
    // GET <GraphController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7/crops
    [HttpGet("{profileUuid:length(32)}/crops")]
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<CropCollectionsDataPointDto>>> GetCropCollections(string playerUuid, string profileUuid, [FromQuery] int days = 7, [FromQuery] long from = 0) {
        var now = DateTimeOffset.UtcNow;
        var start = (from == 0) 
            ? now - TimeSpan.FromDays(days)
            : DateTimeOffset.FromUnixTimeSeconds(from);
        
        var end = (from == 0) 
            ? now
            : DateTimeOffset.FromUnixTimeSeconds(from) + TimeSpan.FromDays(days);

        if (start > end) return BadRequest("Start time cannot be greater than end time.");
        if (end - start > TimeSpan.FromDays(30)) return BadRequest("Time range cannot be greater than 30 days.");
        
        var profile = await _context.ProfileMembers.AsNoTracking()
            .Where(m => m.PlayerUuid == playerUuid && m.ProfileId == profileUuid)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (profile == Guid.Empty) return NotFound("Profile not found.");
        
        var cropCollections = await _timescaleService.GetCropCollections(profile, start, end);
        
        return Ok(cropCollections);
    }
    
    /// <summary>
    /// Skill XP Over Time
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="profileUuid"></param>
    /// <param name="from">Unix timestamp in seconds for the start of the data to return</param>
    /// <param name="perDay">Data points returned per 24 hour period</param>
    /// <response code="200">Returns the list of data points</response>
    /// <returns></returns>
    // GET <GraphController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7/skills
    [HttpGet("{profileUuid:length(32)}/skills")]
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<CropCollectionsDataPointDto>>> GetSkillExperiences(string playerUuid, string profileUuid, [FromQuery] int days = 7, [FromQuery] long from = 0) {
        var now = DateTimeOffset.UtcNow;
        var start = (from == 0) 
            ? now - TimeSpan.FromDays(days)
            : DateTimeOffset.FromUnixTimeSeconds(from);
        
        var end = (from == 0) 
            ? now
            : DateTimeOffset.FromUnixTimeSeconds(from) + TimeSpan.FromDays(days);

        if (start > end) return BadRequest("Start time cannot be greater than end time.");
        if (end - start > TimeSpan.FromDays(30)) return BadRequest("Time range cannot be greater than 30 days.");
        
        var profile = await _context.ProfileMembers.AsNoTracking()
            .Where(m => m.PlayerUuid == playerUuid && m.ProfileId == profileUuid)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (profile == Guid.Empty) return NotFound("Profile not found.");
        
        var cropCollections = await _timescaleService.GetSkills(profile, start, end);
        
        return Ok(cropCollections);
    }

    /// <summary>
    /// Admin: Crop Collections Over Time
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="profileUuid"></param>
    /// <param name="days"></param>
    /// <param name="from"></param>
    /// <returns></returns>
    // GET <GraphController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7/crops
    [Route("/[controller]/Admin/{playerUuid:length(32)}/{profileUuid:length(32)}/crops")]
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<CropCollectionsDataPointDto>>> GetAllCropCollections(string playerUuid, string profileUuid, [FromQuery] int days = 7, [FromQuery] long from = 0) {
        if (!HttpContext.Items.TryGetValue("Account", out var accountItem) || accountItem is not EliteAccount account) {
            return Unauthorized("You do not have permission to view this resource.");
        }

        if (account.Permissions < PermissionFlags.ViewGraphs) {
            return Unauthorized("You do not have permission to view this resource.");
        }
        
        var now = DateTimeOffset.UtcNow;
        var start = (from == 0) 
            ? now - TimeSpan.FromDays(days)
            : DateTimeOffset.FromUnixTimeSeconds(from);
        
        var end = (from == 0) 
            ? now
            : DateTimeOffset.FromUnixTimeSeconds(from) + TimeSpan.FromDays(days);

        if (start > end) return BadRequest("Start time cannot be greater than end time.");

        var profile = await _context.ProfileMembers.AsNoTracking()
            .Where(m => m.PlayerUuid == playerUuid && m.ProfileId == profileUuid)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (profile == Guid.Empty) return NotFound("Profile not found.");
        
        var cropCollections = await _timescaleService.GetCropCollections(profile, start, end, -1);
        
        return Ok(cropCollections);
    }
    
    /// <summary>
    /// Admin: Skill XP Over Time
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="profileUuid"></param>
    /// <param name="days"></param>
    /// <param name="from"></param>
    /// <returns></returns>
    // GET <GraphController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7/skills
    [Route("/[controller]/Admin/{playerUuid:length(32)}/{profileUuid:length(32)}/skills")]
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<SkillsDataPointDto>>> GetAllSkillExperiences(string playerUuid, string profileUuid, [FromQuery] int days = 7, [FromQuery] long from = 0) {
        if (!HttpContext.Items.TryGetValue("Account", out var accountItem) || accountItem is not EliteAccount account) {
            return Unauthorized("You do not have permission to view this resource.");
        }

        if (account.Permissions < PermissionFlags.ViewGraphs) {
            return Unauthorized("You do not have permission to view this resource.");
        }
        
        var now = DateTimeOffset.UtcNow;
        var start = (from == 0) 
            ? now - TimeSpan.FromDays(days)
            : DateTimeOffset.FromUnixTimeSeconds(from);
        
        var end = (from == 0) 
            ? now
            : DateTimeOffset.FromUnixTimeSeconds(from) + TimeSpan.FromDays(days);

        if (start > end) return BadRequest("Start time cannot be greater than end time.");

        var profile = await _context.ProfileMembers.AsNoTracking()
            .Where(m => m.PlayerUuid == playerUuid && m.ProfileId == profileUuid)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        
        if (profile == Guid.Empty) return NotFound("Profile not found.");
        
        var skills = await _timescaleService.GetSkills(profile, start, end, -1);
        
        return Ok(skills);
    }
}