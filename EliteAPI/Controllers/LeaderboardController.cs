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

    // GET: api/<LeaderboardController>
    [HttpGet]
    public async Task<ActionResult<LeaderboardDto<double>>> Get()
    {
        var lb = _settings.Leaderboards.FirstOrDefault(x => x.Id == "FarmingWeight");
        
        if (lb is null)
        {
            return BadRequest("Leaderboard not found");
        }
        
        var data = await _leaderboardService.GetLeaderboardSlice(lb.Id, 0, 100);
        
        if (data.Count == 0)
        {
            return BadRequest("Failed to fetch leaderboard data");
        }

        var leaderboard = new LeaderboardDto<double> {
            Id = lb.Id,
            Title = lb.Title,
            Limit = 100,
            Offset = 0,
            Entries = data
        };
        
        return Ok(leaderboard);
    }

    // GET api/<LeaderboardController>/skill/farming
    [HttpGet("skill/{skillName}")]
    public async Task<object> Get(string skillName)
    {
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
            "runecrafting" => SkillName.RuneCrafting,
            "taming" => SkillName.Taming,
            _ => null
        };

        if (skill is null)
        {
            return "Invalid skill name";
        }

        // SQL query to get the top 100 players for a skill, from the Skills table, with the column name being the skill name
        var data = await _context.Skills
            .Select(s => s.Farming)
            .Where(s => s > 0)
            .OrderByDescending(s => s)
            .Take(100)
            //.Include(s => s.ProfileMember)
            .ToListAsync();

        return data;
    }
}
