using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EliteAPI.Controllers.Contests; 

[Route("/Contests/{playerUuid:length(32)}")]
[ApiController]
public class PlayerContestsController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    
    public PlayerContestsController(DataContext dataContext, IMapper mapper)
    {
        _context = dataContext;
        _mapper = mapper;
    }
    
    // GET <ContestsController>/7da0c47581dc42b4962118f8049147b7/
    [HttpGet]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<ContestParticipationDto>>> GetAllOfOnePlayersContests(string playerUuid)
    {
        var profileMembers = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid))
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .ToListAsync();

        if (profileMembers.Count == 0) return NotFound("Player not found.");

        var data = new List<ContestParticipationDto>();

        foreach (var profileMember in profileMembers)
        {
            data.AddRange(_mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
        }

        return Ok(data);
    }

    // GET <ContestsController>/7da0c47581dc42b4962118f8049147b7/7da0c47581dc42b4962118f8049147b7
    [HttpGet("{profileUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<ContestParticipationDto>>> GetAllContestsOfOneProfileMember(string playerUuid, string profileUuid)
    {
        var profileMember = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.ProfileId.Equals(profileUuid))
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (profileMember is null) return NotFound("Player not found.");

        return Ok(_mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
    }

    // GET <ContestsController>/7da0c47581dc42b4962118f8049147b7/Selected
    [HttpGet("Selected")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<ContestParticipationDto>>> GetAllContestsOfSelectedProfileMember(string playerUuid)
    {
        var profileMember = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (profileMember is null) return NotFound("Player not found.");

        return Ok(_mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
    }
}
