using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Contests; 

[Route("/Graph/Medals")]
[ApiController]
public class MedalGraphsController(DataContext context) : ControllerBase {
    private static readonly JsonSerializerOptions Options = new() {
        PropertyNameCaseInsensitive = true
    };

    [HttpGet("now")]
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<ContestBracketsDetailsDto>> GetMedalBrackets([FromQuery] int months = 2) {
        switch (months) {
            case < 1:
                return BadRequest("Months cannot be less than 1.");
            case > 12:
                return BadRequest("Months cannot be greater than 12.");
        }

        var end = SkyblockDate.Now;
        var start = new SkyblockDate(end.Year - 1, end.Month - months, end.Day).UnixSeconds;
        
        var brackets = await GetAverageMedalBrackets(context, start, end.UnixSeconds);

        return Ok(new ContestBracketsDetailsDto {
            Start = start.ToString(),
            End = end.UnixSeconds.ToString(),
            Brackets = brackets ?? new Dictionary<string, ContestBracketsDto>()
        });
    }
    
    [HttpGet("{sbYear:int}/{sbMonth:int}")]
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<ContestBracketsDetailsDto>> GetMedalBrackets(int sbYear, int sbMonth, [FromQuery] int months = 2) {
        if (sbYear < 1) {
            return BadRequest("Year cannot be less than 1.");
        }
        
        switch (sbMonth) {
            case < 1:
                return BadRequest("Month cannot be less than 1.");
            case > 12:
                return BadRequest("Month cannot be greater than 12.");
        }

        switch (months) {
            case < 1:
                return BadRequest("Months cannot be less than 1.");
            case > 12:
                return BadRequest("Months cannot be greater than 12.");
        }

        var start = new SkyblockDate(sbYear - 1, sbMonth - months, 0).UnixSeconds;
        var end = new SkyblockDate(sbYear - 1, sbMonth, 0).UnixSeconds;
        
        var brackets = await GetAverageMedalBrackets(context, start, end);

        return Ok(new ContestBracketsDetailsDto {
            Start = start.ToString(),
            End = end.ToString(),
            Brackets = brackets ?? new(),
        });
    }

    private static async Task<Dictionary<string, ContestBracketsDto>?> GetAverageMedalBrackets(DbContext context, long start, long end) {
        var medals = await context.Database
            .SqlQuery<string>($@"
                SELECT json_agg(c) AS ""Value""
                FROM (
                   WITH brackets AS (
                       SELECT
                           ""JacobContestId"" AS contest_id,
                           ""JacobContestId"" % 10 AS crop,
                           MIN(""Collected"") filter (where ""MedalEarned"" = 5) AS diamond,
                           MIN(""Collected"") filter (where ""MedalEarned"" = 4) AS platinum,
                           MIN(""Collected"") filter (where ""MedalEarned"" = 3) AS gold,
                           MIN(""Collected"") filter (where ""MedalEarned"" = 2) AS silver,
                           MIN(""Collected"") filter (where ""MedalEarned"" = 1) AS bronze
                       FROM ""ContestParticipations""
                       WHERE ""MedalEarned"" > 0
                       GROUP BY ""JacobContestId""
                   )
                   SELECT
                       crop,
                       AVG(diamond) as diamond,
                       AVG(platinum) as platinum,
                       AVG(gold) as gold,
                       AVG(silver) as silver,
                       AVG(bronze) as bronze
                   FROM brackets
                   WHERE contest_id >= {start} AND contest_id <= {end}
                   GROUP BY crop
                ) c
            ")
            .ToListAsync();

        try {
            var parsed = JsonSerializer.Deserialize<List<MedalCutoffsDbDto>>(medals.First(), Options);

            var dto = parsed!.ToDictionary(
                m => ((Crop)m.Crop).SimpleName(),
                m => new ContestBracketsDto {
                    Diamond = (int) (m.Diamond ?? 0),
                    Platinum = (int) (m.Platinum ?? 0),
                    Gold = (int) (m.Gold ?? 0),
                    Silver = (int) (m.Silver ?? 0),
                    Bronze = (int) (m.Bronze ?? 0)
                });
            
            // Add missing crops to the dictionary
            if (dto.Count < 9) {
                foreach (var crop in Enum.GetValues<Crop>()) {
                    var name = crop.SimpleName();
                    dto.TryAdd(name, new ContestBracketsDto());
                }
            }
            
            return dto!;
        }
        catch 
        {
            return null;
        }
    }
}