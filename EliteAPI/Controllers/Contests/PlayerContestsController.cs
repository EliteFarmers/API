using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EliteAPI.Controllers.Contests; 

[ApiController, ApiVersion(1.0)]
[Route("/contests/{playerUuid:length(32)}")]
[Route("/v{version:apiVersion}/contests/{playerUuid:length(32)}")]
public class PlayerContestsController(
    DataContext dataContext, 
    IMapper mapper) 
    : ControllerBase 
{
    /// <summary>
    /// Get all contests of a player
    /// </summary>
    /// <param name="playerUuid">Player UUID (no hyphens)</param>
    /// <returns></returns>
    [HttpGet]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<ContestParticipationDto>>> GetAllOfOnePlayersContests(string playerUuid)
    {
        var profileMembers = await dataContext.ProfileMembers
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
            data.AddRange(mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
        }

        return Ok(data);
    }

    /// <summary>
    /// Get all contests for a profile member
    /// </summary>
    /// <param name="playerUuid">Player UUID (no hyphens)</param>
    /// <param name="profileUuid">Profile UUID (no hyphens)</param>
    /// <returns></returns>
    [HttpGet("{profileUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<ContestParticipationDto>>> GetAllContestsOfOneProfileMember(string playerUuid, string profileUuid)
    {
        var profileMember = await dataContext.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.ProfileId.Equals(profileUuid))
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (profileMember is null) return NotFound("Player not found.");

        return Ok(mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
    }

    /// <summary>
    /// Get all contests for a selected profile member
    /// </summary>
    /// <param name="playerUuid">Player UUID (no hyphens)</param>
    /// <returns></returns>
    [HttpGet("Selected")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<IEnumerable<ContestParticipationDto>>> GetAllContestsOfSelectedProfileMember(string playerUuid)
    {
        var profileMember = await dataContext.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (profileMember is null) return NotFound("Player not found.");

        return Ok(mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
    }
}
