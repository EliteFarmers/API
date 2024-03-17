using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class BadgeController(DataContext context, IMapper mapper) : ControllerBase {
    
    [Route("/[controller]s")]
    [HttpGet]
    [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<BadgeDto>> GetBadges() {
        var badges = context.Badges
            .AsNoTracking()
            .ToList();
        
        return Ok(mapper.Map<List<BadgeDto>>(badges));
    }
    
}