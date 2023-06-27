using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeightController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public WeightController(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET api/<WeightController>/7da0c47581dc42b4962118f8049147b7/
        [HttpGet("{playerUuid}")]
        public async Task<ActionResult<List<FarmingWeightWithProfileDto>>> GetPlayersProfilesWeight(string playerUuid)
        {
            var uuid = playerUuid.Replace("-", "");

            var farmingWeightIds = await _context.ProfileMembers
                .Where(x => x.PlayerUuid.Equals(uuid))
                .Include(x => x.FarmingWeight)
                .Select(f => f.FarmingWeight.Id)
                .ToListAsync();

            var farmingWeights = await _context.FarmingWeights
                .Where(x => farmingWeightIds.Contains(x.Id))
                .Include(x => x.ProfileMember)
                .ThenInclude(m => m!.Profile)
                .ToListAsync();

            if (farmingWeights.Count == 0)
            {
                return NotFound("No profiles for the player matching this UUID was found");
            }

            return Ok(_mapper.Map<List<FarmingWeightWithProfileDto>>(farmingWeights));
        }

        // GET api/<WeightController>/7da0c47581dc42b4962118f8049147b7/Selected
        [HttpGet("{playerUuid}/Selected")]
        public async Task<ActionResult<FarmingWeightDto>> GetSelectedProfileWeight(string playerUuid)
        {
            var uuid = playerUuid.Replace("-", "");

            var member = await _context.ProfileMembers
                .Where(x => x.PlayerUuid.Equals(uuid) && x.IsSelected)
                .Include(x => x.FarmingWeight)
                .FirstOrDefaultAsync();

            if (member?.FarmingWeight is null)
            {
                return NotFound("No farming weight for the player matching this UUID was found");
            }

            return Ok(_mapper.Map<FarmingWeightDto>(member.FarmingWeight));
        }

        // GET api/<WeightController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7
        [HttpGet("{playerUuid}/{profileUuid}")]
        public async Task<ActionResult<FarmingWeightDto>> GetSpecificProfileWeight(string playerUuid, string profileUuid)
        {
            var uuid = playerUuid.Replace("-", "");
            var profile = profileUuid.Replace("-", "");

            var member = await _context.ProfileMembers
                .Where(x => x.PlayerUuid.Equals(uuid) && x.ProfileId.Equals(profile))
                .Include(x => x.FarmingWeight)
                .FirstOrDefaultAsync();

            if (member?.FarmingWeight is null)
            {
                return NotFound("No farming weight for the player matching this UUID was found");
            }

            return Ok(_mapper.Map<FarmingWeightDto>(member.FarmingWeight));
        }
    }
}
