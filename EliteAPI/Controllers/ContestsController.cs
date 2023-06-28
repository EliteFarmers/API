using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContestsController : ControllerBase
{

    private readonly DataContext _context;
    private readonly IMapper _mapper;

    public ContestsController(DataContext dataContext, IMapper mapper)
    {
        _context = dataContext;
        _mapper = mapper;
    }

    // GET api/<ContestsController>/285
    [HttpGet("at/{year:int}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<Dictionary<long, List<string>>>> GetAllContestsInOneYear(int year)
    {
        var startTime = FormatUtils.GetTimeFromSkyblockDate(year, 0, 0);
        var endTime = FormatUtils.GetTimeFromSkyblockDate(year + 1, 0, 0);
        
        var contests = await _context.JacobContests
            .Where(j => j.Timestamp >= startTime && j.Timestamp < endTime)
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

        return Ok(result);
    }

    // GET api/<ContestsController>/200/12/5
    [HttpGet("at/{year:int}/{month:int}/{day:int}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<IEnumerable<JacobContestWithParticipationsDto>> GetContestsAt(int year, int month, int day)
    {
        var timestamp = FormatUtils.GetTimeFromSkyblockDate(year, month, day);

        return await GetContestsAt(timestamp);
    }

    // GET api/<ContestsController>/200/12
    [HttpGet("at/{year:int}/{month:int}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<Dictionary<int, List<JacobContestDto>>> GetAllContestsInOneMonth(int year, int month)
    {
        var startTime = FormatUtils.GetTimeFromSkyblockDate(year, month, 0);
        var endTime = FormatUtils.GetTimeFromSkyblockDate(year, month + 1, 0);

        var contests = await _context.JacobContests
            .Where(j => j.Timestamp >= startTime && j.Timestamp < endTime)
            .ToListAsync();

        var mappedContests = _mapper.Map<List<JacobContestDto>>(contests);

        var data = new Dictionary<int, List<JacobContestDto>>();

        foreach (var contest in mappedContests)
        {
            var skyblockDate = FormatUtils.GetSkyblockDate(new DateTime().AddSeconds(contest.Timestamp));
            var day = skyblockDate.Day;

            if (data.TryGetValue(day, out var value))
            {
                value.Add(contest);
            }
            else
            {
                data.Add(day, new List<JacobContestDto> { contest });
            }
        }

        return data;
    }

    // GET api/<ContestsController>/1604957700
    [HttpGet("{timestamp:long}")]
    [ResponseCache(Duration = 60 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<IEnumerable<JacobContestWithParticipationsDto>> GetContestsAt(long timestamp)
    {
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
           
            data.First(d => d.Crop.Equals(crop)).Participations = stripped;
        }

        return data;
    }

    // GET api/<ContestsController>/7da0c47581dc42b4962118f8049147b7/
    [HttpGet("{playerUuid}")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<IEnumerable<ContestParticipationDto>> GetAllOfOnePlayersContests(string playerUuid)
    {
        var profileMembers = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid))
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .ToListAsync();

        if (profileMembers.Count == 0) return new List<ContestParticipationDto>();

        var data = new List<ContestParticipationDto>();

        foreach (var profileMember in profileMembers)
        {
            data.AddRange(_mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
        }

        return data;
    }

    // GET api/<ContestsController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7
    [HttpGet("{playerUuid}/{profileUuid}")]
    public async Task<IEnumerable<ContestParticipationDto>> GetAllContestsOfOneProfileMember(string playerUuid, string profileUuid)
    {
        var profileMember = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.ProfileId.Equals(profileUuid))
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (profileMember is null) return new List<ContestParticipationDto>();

        return _mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests);
    }

    // GET api/<ContestsController>/7da0c47581dc42b4962118f8049147b7/Selected
    [HttpGet("{playerUuid}/Selected")]
    public async Task<IEnumerable<ContestParticipationDto>> GetAllContestsOfSelectedProfileMember(string playerUuid)
    {
        var profileMember = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (profileMember is null) return new List<ContestParticipationDto>();

        return _mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests);
    }
}
