using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Farming;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Controllers;

[ApiController, ApiVersion(1.0)]
[Route("[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public class WeightController(
    DataContext context,
    IMapper mapper,
    IConnectionMultiplexer redis,
    IOptions<ConfigFarmingWeightSettings> weightSettings,
    IMemberService memberService) 
    : ControllerBase 
{
    private readonly ConfigFarmingWeightSettings _weightSettings = weightSettings.Value;

    /// <summary>
    /// Get farming weight for all profiles of a player
    /// </summary>
    /// <param name="playerUuid">Player UUID</param>
    /// <param name="collections"></param>
    /// <returns></returns>
    [HttpGet("{playerUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<FarmingWeightAllProfilesDto>> GetPlayersProfilesWeight(string playerUuid, [FromQuery] bool collections = false)
    {
        var uuid = playerUuid.Replace("-", "");
        await memberService.UpdatePlayerIfNeeded(uuid, 3);

        var members = await context.ProfileMembers
            .AsNoTracking()
            .Where(x => x.PlayerUuid.Equals(uuid) && !x.WasRemoved)
            .Include(x => x.Farming)
            .Include(x => x.Profile)
            .ToListAsync();
        
        if (members.Count == 0)
        {
            return NotFound("No profiles for the player matching this UUID was found");
        }

        var dto = new FarmingWeightAllProfilesDto {
            SelectedProfileId = members.FirstOrDefault(p => p.IsSelected)?.ProfileId,
            Profiles = members.Select(m => {
                var mapped = mapper.Map<FarmingWeightWithProfileDto>(m);
                if (collections) {
                    mapped.Crops = m.ExtractCropCollections()
                        .ToDictionary(k => k.Key.ProperName(), v => v.Value);
                }
                return mapped;
            }).ToList()
        };
        
        // TODO: Remove this check after the next SkyHanni full release
        // Check for user agent (ex: "SkyHanni/0.28.Beta.15") with a version lower than "0.28.Beta.14" since it errors with the mouse property
        if (Request.Headers.TryGetValue("User-Agent", out var userAgent) && userAgent.ToString().Contains("SkyHanni"))
        {
            try {
                var version = userAgent.ToString().Split("/")[1].Replace("Beta.", "");
                if (Version.Parse(version) < Version.Parse("0.28.14")) {
                    foreach (var profile in dto.Profiles) {
                        profile.Pests.Mouse = null; // Remove mouse from the response
                    }
                }
            } catch (Exception) {
                return dto;
            }
        }
        
        return dto;
    }

    /// <summary>
    /// Get farming weight for the selected profile of a player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="collections"></param>
    /// <returns></returns>
    [HttpGet("{playerUuid}/Selected")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<FarmingWeightDto>> GetSelectedProfileWeight(string playerUuid, [FromQuery] bool collections = false)
    {
        var uuid = playerUuid.Replace("-", "");
        
        var query = await memberService.ProfileMemberQuery(uuid);
        if (query is null) return NotFound("No profiles for the player matching this UUID was found");

        var weight = await query
            .Where(x => x.IsSelected)
            .Include(x => x.Farming)
            .FirstOrDefaultAsync();
        
        if (weight is null) return NotFound("No farming weight for the player matching this UUID was found");
        
        var mapped = mapper.Map<FarmingWeightDto>(weight.Farming);
        if (!collections) return Ok(mapped);

        mapped.Crops = weight.ExtractCropCollections()
            .ToDictionary(k => k.Key.ProperName(), v => v.Value);
        
        return Ok(mapped);
    }

    /// <summary>
    /// Get farming weight for a specific profile of a player
    /// </summary>
    /// <param name="playerUuid"></param>
    /// <param name="profileUuid"></param>
    /// <param name="collections"></param>
    /// <returns></returns>
    [HttpGet("{playerUuid}/{profileUuid}")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<FarmingWeightDto>> GetSpecificProfileWeight(string playerUuid, string profileUuid, [FromQuery] bool collections = false)
    {
        var uuid = playerUuid.Replace("-", "");
        var profile = profileUuid.Replace("-", "");

        var query = await memberService.ProfileMemberQuery(uuid);
        if (query is null) return NotFound("No profiles for the player matching this UUID was found");

        var weight = await query
            .Where(x => x.IsSelected && x.ProfileId.Equals(profile))
            .Include(x => x.Farming)
            .FirstOrDefaultAsync();
        
        if (weight is null) return NotFound("No farming weight for the player matching this UUID was found");
        
        var mapped = mapper.Map<FarmingWeightDto>(weight.Farming);
        if (!collections) return Ok(mapped);

        mapped.Crops = weight.ExtractCropCollections()
            .ToDictionary(k => k.Key.ProperName(), v => v.Value);
        
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get crop weight constants
    /// </summary>
    /// <remarks>Use /weights/all instead</remarks>
    /// <returns></returns>
    [Obsolete("Use /weights/all instead")]
    [HttpGet]
    [Route("/[controller]s")]
    [Route("/v{version:apiVersion}/[controller]s")]
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
    [HttpGet]
    [Route("/[controller]s/all")]
    [Route("/v{version:apiVersion}/[controller]s/all")]
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
        
        var reversed = FarmingWeightConfig.Settings.PestDropBrackets
            .DistinctBy(p => p.Value)
            .ToDictionary(pair => pair.Value, pair => pair.Key);

        var result = new WeightsDto {
            Crops = crops,
            Pests = {
                Brackets = FarmingWeightConfig.Settings.PestDropBrackets,
                Values = FarmingWeightConfig.Settings.PestCropDropChances
                    .DistinctBy(p => p.Key.ToString().ToLowerInvariant())
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
