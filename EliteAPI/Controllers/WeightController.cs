using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.ProfileService;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class WeightController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly ConfigCooldownSettings _coolDowns;
    private readonly IProfileService _profileService;
    private readonly ConfigFarmingWeightSettings _weightSettings;

    public WeightController(DataContext context, IMapper mapper, IProfileService profileService, 
        IOptions<ConfigCooldownSettings> coolDowns, IOptions<ConfigFarmingWeightSettings> weightSettings)
    {
        _context = context;
        _mapper = mapper;
        _profileService = profileService;
        _coolDowns = coolDowns.Value;
        _weightSettings = weightSettings.Value;
    }

    // GET <WeightController>/7da0c47581dc42b4962118f8049147b7/
    [HttpGet("{playerUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<FarmingWeightAllProfilesDto>> GetPlayersProfilesWeight(string playerUuid)
    {
        var uuid = playerUuid.Replace("-", "");

        var members = await _context.ProfileMembers
            .Where(x => x.PlayerUuid.Equals(uuid))
            .Include(x => x.FarmingWeight)
            .ToListAsync();

        var farmingWeightIds = new List<int>();
        
        // Check if any of the profiles are old
        if (members.Count == 0 || members.Exists(member => member.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown))) {
            await _profileService.GetSelectedProfileMember(playerUuid);
            
            farmingWeightIds = await _context.ProfileMembers
                .Where(x => x.PlayerUuid.Equals(uuid))
                .Include(x => x.FarmingWeight)
                .Select(x => x.FarmingWeight.Id)
                .ToListAsync();
        } else {
            farmingWeightIds = members.Select(x => x.FarmingWeight.Id).ToList();
        }

        var farmingWeights = await _context.FarmingWeights
            .Where(x => farmingWeightIds.Contains(x.Id))
            .Include(x => x.ProfileMember)
            .ThenInclude(m => m!.Profile)
            .ToListAsync();

        if (farmingWeights.Count == 0)
        {
            return NotFound("No profiles for the player matching this UUID was found");
        }

        var dto = new FarmingWeightAllProfilesDto {
            SelectedProfileId = farmingWeights
                .FirstOrDefault(w => w.ProfileMember?.IsSelected ?? false)?.ProfileMember?.ProfileId,
            Profiles = _mapper.Map<List<FarmingWeightWithProfileDto>>(farmingWeights)
        };

        return Ok(dto);
    }

    // GET <WeightController>/7da0c47581dc42b4962118f8049147b7/Selected
    [HttpGet("{playerUuid}/Selected")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<FarmingWeightDto>> GetSelectedProfileWeight(string playerUuid)
    {
        var uuid = playerUuid.Replace("-", "");

        var member = await _context.ProfileMembers
            .Where(x => x.PlayerUuid.Equals(uuid) && x.IsSelected)
            .Include(x => x.FarmingWeight)
            .FirstOrDefaultAsync();

        if (member?.FarmingWeight is null || member.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown))
        {
            var newMember = await _profileService.GetSelectedProfileMember(playerUuid);
            
            if (newMember is null) {
                return NotFound("No farming weight for the player matching this UUID was found");
            }
            
            return Ok(_mapper.Map<FarmingWeightDto>(newMember.FarmingWeight));
        }

        return Ok(_mapper.Map<FarmingWeightDto>(member.FarmingWeight));
    }

    // GET <WeightController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7
    [HttpGet("{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<FarmingWeightDto>> GetSpecificProfileWeight(string playerUuid, string profileUuid)
    {
        var uuid = playerUuid.Replace("-", "");
        var profile = profileUuid.Replace("-", "");

        var member = await _context.ProfileMembers
            .Where(x => x.PlayerUuid.Equals(uuid) && x.ProfileId.Equals(profile))
            .Include(x => x.FarmingWeight)
            .FirstOrDefaultAsync();

        if (member?.FarmingWeight is null || member.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown))
        {
            var newMember = await _profileService.GetSelectedProfileMember(playerUuid);
            
            if (newMember is null) {
                return NotFound("No farming weight for the player matching this UUID was found");
            }
            
            return Ok(_mapper.Map<FarmingWeightDto>(newMember.FarmingWeight));
        }

        return Ok(_mapper.Map<FarmingWeightDto>(member.FarmingWeight));
    }
    
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    [DisableRateLimiting]
    public ActionResult<Dictionary<string, double>> GetCropWeights() {
        var rawWeights = _weightSettings.CropsPerOneWeight;
        
        var weights = new Dictionary<string, double>();
        
        foreach (var (key, value) in rawWeights) {
            var formattedKey = FormatUtils.GetFormattedCropName(key);
            
            if (formattedKey is null) continue;
            
            weights.Add(formattedKey, value);
        }

        return Ok(weights);
    }
}
