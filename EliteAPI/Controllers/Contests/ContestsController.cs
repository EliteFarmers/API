using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;

namespace EliteAPI.Controllers.Contests;

[Route("[controller]")]
[ApiController]
public class ContestsController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IConnectionMultiplexer _cache;
    private readonly ILogger<ContestsController> _logger;

    public ContestsController(DataContext dataContext, IMapper mapper, IConnectionMultiplexer cache, ILogger<ContestsController> logger)
    {
        _context = dataContext;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    // GET <ContestsController>/285
    [HttpGet("at/{year:int}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<YearlyContestsDto>> GetAllContestsInOneYear(int year, bool now = false)
    {
        var currentDate = SkyblockDate.Now;

        // Decrease cache time to 2 minutes if it's the end/start of the year in preparation for the next year
        if (now && currentDate is { Month: >= 11, Day: >= 27 } or { Month: 0, Day: <= 2 }) {
            Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
                Public = true,
                MaxAge = TimeSpan.FromMinutes(2)
            };
        }
        
        if (currentDate.Year == year - 1) {
            var db = _cache.GetDatabase();

            var data = await db.StringGetAsync($"contests:{currentDate.Year}");
            if (data.HasValue)
                try {
                    var sourcedContests = JsonSerializer.Deserialize<Dictionary<long, List<string>>>(data!);

                    return Ok(new YearlyContestsDto {
                        Year = currentDate.Year + 1,
                        Count = (sourcedContests?.Count ?? 0) * 3,
                        Complete = sourcedContests?.Count == 124,
                        Contests = sourcedContests ?? new Dictionary<long, List<string>>()
                    });
                }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to deserialize cached contests data");
                }
        }
        
        var startTime = FormatUtils.GetTimeFromSkyblockDate(year - 1, 0, 0);
        var endTime = FormatUtils.GetTimeFromSkyblockDate(year, 0, 0);
        
        var contests = await _context.JacobContests
            .Where(j => j.Timestamp > startTime && j.Timestamp < endTime)
            .ToListAsync();

        var result = new Dictionary<long, List<string>>();
        foreach (var contest in contests) {
            if (!result.TryGetValue(contest.Timestamp, out var value)) {
                value = new List<string>();
                result.Add(contest.Timestamp, value);
            }

            var crop = FormatUtils.GetFormattedCropName(contest.Crop);

            value.Add(crop);
        }

        var dto = new YearlyContestsDto {
            Year = year,
            Count = contests.Count,
            Complete = contests.Count == 372,
            Contests = result
        };

        return Ok(dto);
    }
    
    private async Task<List<ContestParticipationWithTimestampDto>> FetchRecords(Crop crop, long startTime, long endTime) {
        var cropInt = (int)crop;

        try {
            // Work around EF Core not supporting DISTINCT ON
            // Also work around EF not supporting mapping to a DTO by parsing as JSON
            var asJson = await _context.Database.SqlQuery<string>($@"
                SELECT json_agg(c) as ""Value""
                FROM (
                    SELECT ""Collected"", ""Position"", ""Crop"", ""Timestamp"", ""Participants"", ""PlayerUuid"", ""ProfileId"" as ""ProfileUuid"", ""Name"" as ""PlayerName""
                    FROM (
                        SELECT DISTINCT ON (""ProfileMemberId"") ""ProfileMemberId"", ""Collected"", ""Position"", ""Crop"", ""Timestamp"", ""Participants""
                        FROM ""ContestParticipations""
                        LEFT JOIN ""JacobContests"" ON ""JacobContestId"" = ""JacobContests"".""Id""
                        WHERE ""Crop"" = {cropInt} AND ""JacobContestId"" BETWEEN {startTime} AND {endTime}
                        ORDER BY ""ProfileMemberId"", ""Collected"" DESC
                    ) sub
                    LEFT JOIN ""ProfileMembers"" ON sub.""ProfileMemberId"" = ""ProfileMembers"".""Id""
                    LEFT JOIN ""MinecraftAccounts"" ON ""PlayerUuid"" = ""MinecraftAccounts"".""Id""
                    ORDER BY ""Collected"" DESC
                    LIMIT 100
                ) c
            ").FirstOrDefaultAsync();

            if (asJson is null) return new List<ContestParticipationWithTimestampDto>();
            var parsed = JsonSerializer.Deserialize<List<ContestParticipationWithTimestampDto>>(asJson);

            return parsed ?? new List<ContestParticipationWithTimestampDto>();
        } catch (Exception e) {
            _logger.LogError(e, "Failed to deserialize contest records");
            return new List<ContestParticipationWithTimestampDto>();
        }
    }
    
    // GET <ContestsController>/285
    [HttpGet("Records/{year:int}")]
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<YearlyCropRecordsDto>> GetRecordsInOneYear(int year)
    {
        var startTime = FormatUtils.GetTimeFromSkyblockDate(year - 1, 0, 0);
        var endTime = FormatUtils.GetTimeFromSkyblockDate(year, 0, 0);
        
        if (startTime > SkyblockDate.Now.UnixSeconds) {
            return BadRequest("Cannot fetch records for a year that hasn't happened yet!");
        }
        
        var dto = new YearlyCropRecordsDto {
            Year = year,
            Crops = new Dictionary<string, List<ContestParticipationWithTimestampDto>> {
                { "cactus", await FetchRecords(Crop.Cactus, startTime, endTime) },
                { "carrot", await FetchRecords(Crop.Carrot, startTime, endTime) },
                { "potato", await FetchRecords(Crop.Potato, startTime, endTime) },
                { "pumpkin", await FetchRecords(Crop.Pumpkin, startTime, endTime) },
                { "melon", await FetchRecords(Crop.Melon, startTime, endTime) },
                { "mushroom", await FetchRecords(Crop.Mushroom, startTime, endTime) },
                { "cocoa", await FetchRecords(Crop.CocoaBeans, startTime, endTime) },
                { "cane", await FetchRecords(Crop.SugarCane, startTime, endTime) },
                { "wart", await FetchRecords(Crop.NetherWart, startTime, endTime) },
                { "wheat", await FetchRecords(Crop.Wheat, startTime, endTime) }
            }
        };

        return Ok(dto);
    }
    
    // GET <ContestsController>/at/now
    [HttpGet("at/now")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<YearlyContestsDto>> GetThisYearsContests() {
        return await GetAllContestsInOneYear(SkyblockDate.Now.Year + 1, true);
    }
    
    // GET <ContestsController>/200/12/5
    [HttpGet("at/{year:int}/{month:int}/{day:int}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<JacobContestWithParticipationsDto>>> GetContestsAt(int year, int month, int day) {
        if (year < 1 || month is > 12 or < 1 || day is > 31 or < 1) return BadRequest("Invalid date.");
        
        var timestamp = FormatUtils.GetTimeFromSkyblockDate(year - 1, month - 1, day - 1);

        return await GetContestsAt(timestamp);
    }

    // GET <ContestsController>/200/12
    [HttpGet("at/{year:int}/{month:int}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<Dictionary<int, List<JacobContestDto>>>> GetAllContestsInOneMonth(int year, int month)
    {
        if (year < 1 || month is > 12 or < 1) return BadRequest("Invalid date.");
        
        var startTime = FormatUtils.GetTimeFromSkyblockDate(year - 1, month - 1, 0);
        var endTime = FormatUtils.GetTimeFromSkyblockDate(year - 1, month, 0);

        var contests = await _context.JacobContests
            .Where(j => j.Timestamp >= startTime && j.Timestamp < endTime)
            .ToListAsync();

        var mappedContests = _mapper.Map<List<JacobContestDto>>(contests);

        var data = new Dictionary<int, List<JacobContestDto>>();

        foreach (var contest in mappedContests) {
            var skyblockDate = new SkyblockDate(contest.Timestamp);
            var day = skyblockDate.Day + 1;

            if (data.TryGetValue(day, out var value))
            {
                value.Add(contest);
            }
            else
            {
                data.Add(day, new List<JacobContestDto> { contest });
            }
        }

        return Ok(data);
    }

    // GET <ContestsController>/1604957700
    [HttpGet("{timestamp:long}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<JacobContestWithParticipationsDto>>> GetContestsAt(long timestamp)
    {
        var skyblockDate = new SkyblockDate(timestamp);
        if (skyblockDate.Year < 1) return BadRequest("Invalid skyblock date.");
        
        var contests = await _context.JacobContests
            .Where(j => j.Timestamp == timestamp)
            .ToListAsync();

        if (contests.Count == 0) return new List<JacobContestWithParticipationsDto>();
        
        var data = _mapper.Map<List<JacobContestWithParticipationsDto>>(contests);

        foreach (var contest in contests)
        {
            var participations = await _context.ContestParticipations
                .Where(p => p.JacobContestId == contest.Id)
                .Include(p => p.ProfileMember.MinecraftAccount)
                .ToListAsync();

            var crop = FormatUtils.GetFormattedCropName(contest.Crop);

            var stripped = _mapper.Map<List<StrippedContestParticipationDto>>(participations);

            var contestDto = data.First(d => d.Crop.Equals(crop));
            contestDto.Participations = stripped;
            contestDto.CalculateBrackets();
        }

        return Ok(data);
    }
    
    // GET /contest/285:2_11:CACTUS
    [Route("/Contest/{contestKey}")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<JacobContestWithParticipationsDto>> GetContestFromKey(string contestKey) {
        var timestamp = FormatUtils.GetTimeFromContestKey(contestKey);
        var cropId = FormatUtils.GetCropFromContestKey(contestKey);

        if (timestamp == 0 || cropId is null) {
            return BadRequest("Invalid contest key");
        }
        
        var contest = await _context.JacobContests
            .Where(j => j.Timestamp == timestamp && j.Crop == cropId)
            .FirstOrDefaultAsync();
        
        if (contest is null) return NotFound("Contest not found");
        
        var data = _mapper.Map<JacobContestWithParticipationsDto>(contest);
        
        var participations = await _context.ContestParticipations
            .Where(p => p.JacobContestId == contest.Id)
            .Include(p => p.ProfileMember.MinecraftAccount)
            .ToListAsync();
        
        var stripped = _mapper.Map<List<StrippedContestParticipationDto>>(participations);
        
        data.Participations = stripped;
        return Ok(data);
    }
    
    // POST <ContestsController>/at/now
    [HttpPost("at/now")]
    [RequestSizeLimit(16000)] // Leaves some room for error
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult> SendThisYearsContests([FromBody] Dictionary<long, List<string>> body) {
        var currentDate = new SkyblockDate(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var currentYear = currentDate.Year;
        
        var db = _cache.GetDatabase();
        if (await db.KeyExistsAsync($"contests:{currentDate.Year}")) {
            //return Ok();
        }
        
        if (currentDate.Month > 8) {
            return BadRequest("Contests cannot be submitted this late in the year!");
        }
        
        if (body.Keys.Count != 124 || body.Keys.Distinct().Count() != 124) {
            return BadRequest("Invalid number of contests! Expected 124, got " + body.Count);
        }
        
        // Check if any of the timestamps are invalid
        if (body.Keys.ToList().Exists(timestamp => new SkyblockDate(timestamp).Year != currentYear || DateTimeOffset.FromUnixTimeSeconds(timestamp).Minute != 15)) {
            return BadRequest("Invalid timestamp! All contests must be from the current SkyBlock year (" +
                              (currentYear + 1) + ") and begin on the 15 minute mark!");
        }
        
        // Check if any of the crops are invalid
        if (body.Values.ToList().Exists(crops => // Check that all crops are valid and that there are no duplicates
                crops.Distinct().Count() != 3 ||
                crops.Exists(crop => FormatUtils.FormattedCropNameToCrop(crop) is null))) 
        {
            return BadRequest("Invalid contest(s)! All crops must be valid without duplicates in the same contest!");
        }

        var httpContext = HttpContext.Request.HttpContext;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
        {
            return BadRequest("Invalid request");
        }

        var addressKey = IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress!) 
            ? $"contestsSubmission:{Guid.NewGuid()}" // Use a GUID for localhost so that it can be tested
            : $"contestsSubmission:{ipAddress}";

        var existingData = await db.StringGetAsync(addressKey);
        
        // Check if IP has already submitted a response
        if (!string.IsNullOrEmpty(existingData))
        {
            return BadRequest("Already submitted a response");
        }
        
        // Store that the IP has submitted a response
        await db.StringSetAsync(addressKey, "1", TimeSpan.FromHours(5));
        
        // Serialize the body to a JSON string
        var serializedData = JsonSerializer.Serialize(body);
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(serializedData)));

        // Increment the number of this particular response
        var hashKey = $"contestsHash:{hash}";
        await db.StringIncrementAsync(hashKey);
        await db.StringGetSetExpiryAsync(hashKey, TimeSpan.FromHours(5));
        
        //Get the current number of this particular response
        var identicalResponses = await db.StringGetAsync(hashKey);

        if (!identicalResponses.TryParse(out long val) || val < 5) return Ok($"Response saved, {val} identical responses");
        
        var secondsUntilNextYear = FormatUtils.GetTimeFromSkyblockDate(currentYear + 1, 0, 0) - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Save the request data
        await db.StringSetAsync($"contests:{currentYear}", serializedData, TimeSpan.FromSeconds(secondsUntilNextYear));

        return Ok();
    }
}
