using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers;

[ApiController, ApiVersion(1.0)]
[Route("[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public class BadgeController(
    DataContext context,
    IBadgeService badgeService,
    IObjectStorageService objectStorageService,
    IMapper mapper)
    : ControllerBase
{
    
    /// <summary>
    /// Get all badges
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/[controller]s")]
    [Route("/v{version:apiVersion}/[controller]s")]
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
    public async Task<ActionResult> CreateBadge([FromForm] CreateBadgeDto badge) {
        var newBadge = new Badge {
            Name = badge.Name,
            Description = badge.Description,
            Requirements = badge.Requirements,
            TieToAccount = badge.TieToAccount
        };
        
        context.Badges.Add(newBadge);
        await context.SaveChangesAsync();
        
        if (badge.Image is not null) {
            var image = await objectStorageService.UploadImageAsync($"badges/{newBadge.Id}.png", badge.Image);
           
            newBadge.Image = image;
            newBadge.ImageId = image.Id;
            
            await context.SaveChangesAsync();
        }
        
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
    public async Task<ActionResult> EditBadge(int badgeId, [FromForm] EditBadgeDto badge) {
        var existingBadge = await context.Badges
            .Include(b => b.Image)
            .FirstOrDefaultAsync(b => b.Id == badgeId);
        
        if (existingBadge is null) {
            return NotFound("Badge not found.");
        }
        
        existingBadge.Name = badge.Name ?? existingBadge.Name;
        existingBadge.Description = badge.Description ?? existingBadge.Description;
        existingBadge.Requirements = badge.Requirements ?? existingBadge.Requirements;
        
        if (badge.Image is not null) {
            if (existingBadge.Image is not null) {
                await objectStorageService.DeleteAsync(existingBadge.Image.Path);
            }
            
            var image = await objectStorageService.UploadImageAsync($"badges/{existingBadge.Id}.png", badge.Image);
           
            existingBadge.Image = image;
            existingBadge.ImageId = image.Id;
        }
        
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