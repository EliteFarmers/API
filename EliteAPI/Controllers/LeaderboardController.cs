using EliteAPI.Data;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly DataContext _context;

    public LeaderboardController(DataContext dataContext)
    {
        _context = dataContext;
    }

    // GET: api/<LeaderboardController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
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
