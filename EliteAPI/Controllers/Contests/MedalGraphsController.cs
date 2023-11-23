using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Contests; 

[Route("/Graph/Medals")]
[ApiController]
public class MedalGraphsController(DataContext context) : ControllerBase {
    
    [HttpGet]
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<CropCollectionsDataPointDto>>> GetCropCollections(int startYear, [FromQuery] int years = 1) {
        switch (years) {
            case < 1:
                return BadRequest("Can't have less than 1 year of records.");
            case > 10:
                return BadRequest("Time range cannot be greater than 10 skyblock years.");
        }

        var start = new SkyblockDate(startYear - years - 1, 0, 0).UnixSeconds;
        var end = new SkyblockDate(startYear - 1, 0, 0).UnixSeconds;

        var profile = await context.ContestParticipations.AsNoTracking()
            .Where(c => c.JacobContestId >= start && c.JacobContestId <= end)
            .GroupBy(c => new { c.JacobContestId, c.MedalEarned })
            .Select(c => new {
                Contest = c.Key.JacobContestId,
                Medal = c.Key.MedalEarned,
                Lowest = c.First().Collected
            }).GroupBy(c => c.Contest)
            .ToListAsync();
        //
        // var cropCollections = await _timescaleService.GetCropCollections(profile, start, end);
        //
        return Ok(profile);
    }
    
}