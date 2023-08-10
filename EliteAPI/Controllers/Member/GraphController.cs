using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.MemberService;
using EliteAPI.Services.TimescaleService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Member; 

[Route("[controller]/{playerUuid:length(32)}")]
[ApiController]
public class GraphController : ControllerBase {
    private readonly ITimescaleService _timescaleService;
    private readonly IMemberService _memberService;

    public GraphController(IMemberService memberService, ITimescaleService timescaleService) {
        _memberService = memberService;
        _timescaleService = timescaleService;
    }
    
    // GET <GraphController>/7da0c47581dc42b4962118f8049147b7/crops
    [HttpGet("crops")]
    [ResponseCache(Duration = 60 * 10, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<List<CropCollectionsDataPointDto>>> GetCropCollections(string playerUuid, [FromQuery] string? profileId, [FromQuery] long start = 0, [FromQuery] long end = 0) {
        if (start == 0) start = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        if (end == 0) end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        if (start > end) return BadRequest("Start time cannot be greater than end time.");
        if (end - start > 60 * 60 * 24 * 30) return BadRequest("Time range cannot be greater than 30 days.");
        
        if (profileId is not null && profileId is not { Length: 32 }) return BadRequest("Profile ID cannot be null.");
        
        var query = await _memberService.ProfileMemberQuery(playerUuid);
        if (query is null) return NotFound("Member not found.");

        var profiles = await query
            .Select(p => new { p.Id, p.ProfileId, p.IsSelected })
            .ToListAsync();
        
        var selectedProfile = profiles.FirstOrDefault(p => p.IsSelected);
        
        if (profileId is not null) {
            selectedProfile = profiles.FirstOrDefault(p => p.ProfileId == profileId) ?? selectedProfile;
        }
        
        if (selectedProfile is null) return NotFound("Profile not found.");
        
        var cropCollections = await _timescaleService.GetCropCollections(selectedProfile.Id, DateTimeOffset.FromUnixTimeSeconds(start), DateTimeOffset.FromUnixTimeSeconds(end), 240);
        
        return Ok(cropCollections);
    }   
    
}