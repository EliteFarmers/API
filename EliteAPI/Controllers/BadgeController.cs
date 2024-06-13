using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class BadgeController(
    DataContext context,
    IBadgeService badgeService,
    IMapper mapper)
: ControllerBase
{
    
    /// <summary>
    /// Get all badges
    /// </summary>
    /// <returns></returns>
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
    
    /// <summary>
    /// Add a badge to a user
    /// </summary>
    /// <param name="playerUuid">Player UUID (no hyphens)</param>
    /// <param name="badgeId">Badge ID</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Moderator)]
    [HttpPost("user/{playerUuid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> AddPlayerBadge(string playerUuid, int badgeId) {
        return await badgeService.AddBadgeToUser(playerUuid, badgeId);
    }
    
    /// <summary>
    /// Remove a badge from a user
    /// </summary>
    /// <param name="playerUuid">Player UUID (no hyphens)</param>
    /// <param name="badgeId">Badge ID</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Moderator)]
    [HttpDelete("user/{playerUuid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> RemovePlayerBadge(string playerUuid, int badgeId) {
        return await badgeService.RemoveBadgeFromUser(playerUuid, badgeId);
    }
    
    /// <summary>
    /// Create a new badge
    /// </summary>
    /// <param name="badge">Badge information</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> CreateBadge([FromBody] CreateBadgeDto badge) {
        var newBadge = new Badge {
            Name = badge.Name,
            Description = badge.Description,
            ImageId = badge.ImageId,
            Requirements = badge.Requirements,
            TieToAccount = badge.TieToAccount
        };
        
        context.Badges.Add(newBadge);
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Edit a badge
    /// </summary>
    /// <param name="badgeId">Badge ID</param>
    /// <param name="badge">New values</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPatch("{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> EditBadge(int badgeId, [FromBody] EditBadgeDto badge) {
        var existingBadge = await context.Badges
            .FirstOrDefaultAsync(b => b.Id == badgeId);
        
        if (existingBadge is null) {
            return NotFound("Badge not found.");
        }
        
        existingBadge.Name = badge.Name ?? existingBadge.Name;
        existingBadge.Description = badge.Description ?? existingBadge.Description;
        existingBadge.ImageId = badge.ImageId ?? existingBadge.ImageId;
        existingBadge.Requirements = badge.Requirements ?? existingBadge.Requirements;
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    /// <summary>
    /// Delete a badge
    /// </summary>
    /// <param name="badgeId">Badge ID</param>
    /// <returns></returns>
    [Authorize(ApiUserPolicies.Admin)]
    [HttpDelete("{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> DeleteBadge(int badgeId) {
        var existingBadge = await context.Badges
            .FirstOrDefaultAsync(b => b.Id == badgeId);
        
        if (existingBadge is null) {
            return NotFound("Badge not found.");
        }
        
        context.Badges.Remove(existingBadge);
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
}