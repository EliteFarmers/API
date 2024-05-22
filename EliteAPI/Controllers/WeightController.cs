using System.Text.Json;
using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.MemberService;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class WeightController(
    DataContext context,
    IMapper mapper,
    IConnectionMultiplexer redis,
    IOptions<ConfigFarmingWeightSettings> weightSettings,
    IMemberService memberService
) : ControllerBase 
{
    private readonly ConfigFarmingWeightSettings _weightSettings = weightSettings.Value;

    /// <summary>
    /// Get farming weight for all profiles of a player
    /// </summary>
    /// <param name="playerUuid">Player UUID</param>
    /// <returns></returns>
    // GET <WeightController>/7da0c47581dc42b4962118f8049147b7/
    [HttpGet("{playerUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<FarmingWeightAllProfilesDto>> GetPlayersProfilesWeight(string playerUuid)
    {
        var uuid = playerUuid.Replace("-", "");
        await memberService.UpdatePlayerIfNeeded(uuid);

        var farmingWeightIds = await context.ProfileMembers
            .AsNoTracking()
            .Where(x => x.PlayerUuid.Equals(uuid))
            .Include(x => x.Farming)
            .Select(x => x.Farming.Id)
            .ToListAsync();

        var farmingWeights = await context.Farming
            .AsNoTracking()
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
            Profiles = mapper.Map<List<FarmingWeightWithProfileDto>>(farmingWeights)
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get farming weight for the selected profile of a player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <returns></returns>
    // GET <WeightController>/7da0c47581dc42b4962118f8049147b7/Selected
    [HttpGet("{playerUuid}/Selected")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<FarmingWeightDto>> GetSelectedProfileWeight(string playerUuid)
    {
        var uuid = playerUuid.Replace("-", "");
        
        var query = await memberService.ProfileMemberQuery(uuid);
        if (query is null) return NotFound("No profiles for the player matching this UUID was found");

        var weight = await query
            .Where(x => x.IsSelected)
            .Include(x => x.Farming)
            .Select(x => x.Farming)
            .FirstOrDefaultAsync();
        
        if (weight is null) return NotFound("No farming weight for the player matching this UUID was found");
        
        return Ok(mapper.Map<FarmingWeightDto>(weight));
    }

    /// <summary>
    /// Get farming weight for a specific profile of a player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="profileUuid"></param>
    /// <returns></returns>
    // GET <WeightController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7
    [HttpGet("{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<FarmingWeightDto>> GetSpecificProfileWeight(string playerUuid, string profileUuid)
    {
        var uuid = playerUuid.Replace("-", "");
        var profile = profileUuid.Replace("-", "");

        var query = await memberService.ProfileMemberQuery(uuid);
        if (query is null) return NotFound("No profiles for the player matching this UUID was found");

        var weight = await query
            .Where(x => x.IsSelected && x.ProfileId.Equals(profile))
            .Include(x => x.Farming)
            .Select(x => x.Farming)
            .FirstOrDefaultAsync();
        
        if (weight is null) return NotFound("No farming weight for the player matching this UUID was found");
        
        return Ok(mapper.Map<FarmingWeightDto>(weight));
    }
    
    /// <summary>
    /// Get crop weight constants
    /// </summary>
    /// <remarks>Use /weights/all instead</remarks>
    /// <returns></returns>
    [Obsolete("Use /weights/all instead")]
    [Route("/[controller]s")]
    [HttpGet]
    [DisableRateLimiting]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    
    /// <summary>
    /// Get all farming weight constants
    /// </summary>
    /// <returns></returns>
    [Route("/[controller]s/All")]
    [HttpGet]
    [DisableRateLimiting]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<WeightsDto>> GetWeights() {
        var db = redis.GetDatabase();
        var stored = await db.StringGetAsync("farming:weights");
        
        if (!stored.IsNullOrEmpty) {
            try {
                var storedWeights = JsonSerializer.Deserialize<WeightsDto>(stored!);
                return Ok(storedWeights);
            } catch (JsonException) {
                await db.KeyDeleteAsync("farming:weights");
            }
        }
        
        var rawWeights = _weightSettings.CropsPerOneWeight;
        var crops = new Dictionary<string, double>();
        
        foreach (var (key, value) in rawWeights) {
            var formattedKey = FormatUtils.GetFormattedCropName(key);
            if (formattedKey is null) continue;
            
            crops.Add(formattedKey, value);
        }
        
        var reversed = FarmingItemsConfig.Settings.PestDropBrackets
            .ToDictionary(pair => pair.Value, pair => pair.Key);

        var result = new WeightsDto {
            Crops = crops,
            Pests = {
                Brackets = FarmingItemsConfig.Settings.PestDropBrackets,
                Values = FarmingItemsConfig.Settings.PestCropDropChances
                    .ToDictionary(
                        pair => pair.Key.ToString().ToLowerInvariant(), 
                        pair => pair.Value.GetPrecomputed().ToDictionary(
                            valuePair => reversed[valuePair.Key], 
                            valuePair => valuePair.Value
                        )
                    )
            }
        };

        await db.StringSetAsync("farming:weights", JsonSerializer.Serialize(result), TimeSpan.FromHours(12));
        
        return Ok(result);
    }
}
