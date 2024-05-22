using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Controllers; 

[ApiController]
[Authorize("Moderator")]
public class AdminController(
    DataContext context, 
    IMapper mapper, 
    IConnectionMultiplexer redis,
    UserManager<ApiUser> userManager) 
    : ControllerBase
{
    
    /// <summary>
    /// Get list of members with permissions
    /// </summary>
    /// <returns></returns>
    [HttpGet("Admins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    public async Task<ActionResult<List<AccountWithPermsDto>>> GetAdmins() {
        var members = await context.Accounts
            .AsNoTracking()
            .Where(a => a.Permissions != PermissionFlags.None)
            .ToListAsync();
        
        return Ok(mapper.Map<List<AccountWithPermsDto>>(members));
    }
    
    // POST <AdminController>/Permissions/12793764936498429/17
    /// <summary>
    /// Add member permissions
    /// </summary>
    /// <param name="memberId"></param>
    /// <param name="permission"></param>
    /// <returns></returns>
    [Authorize("Admin")]
    [HttpPost("[controller]/Permissions/{memberId:long}/{permission:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> PromoteMember(long memberId, ushort permission) {
        // Check that permission is valid
        if (!Enum.IsDefined(typeof(PermissionFlags), permission)) {
            return BadRequest("Invalid permission.");
        }

        var member = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == (ulong) memberId);
        
        if (member is null) {
            return NotFound("User not found.");
        }
        
        // Set permission
        member.Permissions = (PermissionFlags) permission;
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    // DELETE <AdminController>/Permissions/12793764936498429/17
    /// <summary>
    /// Remove member permissions
    /// </summary>
    /// <param name="memberId"></param>
    /// <param name="permission"></param>
    /// <returns></returns>
    [Authorize("Admin")]
    [HttpDelete("[controller]/Permissions/{memberId:long}/{permission:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> DemoteMember(long memberId, ushort permission) {
        // Check that permission is valid
        if (!Enum.IsDefined(typeof(PermissionFlags), permission)) {
            return BadRequest("Invalid permission.");
        }
        
        var member = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == (ulong) memberId);
        
        if (member is null) {
            return NotFound("User not found.");
        }
        
        // Remove permission
        member.Permissions = PermissionFlags.None;
        
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    
    /// <summary>
    /// Get list of roles
    /// </summary>
    /// <returns></returns>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    public async Task<string[]> GetRoles() {
        return await context.Roles.AsNoTracking()
            .Select(r => r.Name)
            .Where(r => r != null)
            .ToArrayAsync() as string[];
    }

    /// <summary>
    /// Add a role to a user
    /// </summary>
    /// <param name="userId">Member id</param>
    /// <param name="role">Role name</param>
    /// <returns></returns>
    [Authorize("Admin")]
    [HttpPost("[controller]/User/{userId}/Roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> AddRoleToUser(string userId, string role) {
        // Check that the role exists in the role database
        var existingRole = await context.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == role);
        
        if (existingRole is null) {
            return NotFound("Role not found.");
        }
        
        var user = await userManager.FindByIdAsync(userId);
        
        if (user is null) {
            return NotFound("User not found.");
        }
        
        // Add role to user
        var result = await userManager.AddToRoleAsync(user, role);
        
        if (!result.Succeeded) {
            return BadRequest("Failed to add role.");
        }
        
        return Ok();
    }

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    /// <param name="userId">Member id</param>
    /// <param name="role">Role name</param>
    /// <returns></returns>
    [Authorize("Admin")]
    [HttpDelete("[controller]/User/{userId}/Roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> RemoveRoleFromUser(string userId, string role) {
        // Check that the role exists in the role database
        var existingRole = await context.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == role);
        
        if (existingRole is null) {
            return NotFound("Role not found.");
        }
        
        var user = await userManager.FindByIdAsync(userId);
        
        if (user is null) {
            return NotFound("User not found.");
        }
        
        // Add role to user
        var result = await userManager.RemoveFromRoleAsync(user, role);
        
        if (!result.Succeeded) {
            return BadRequest("Failed to add role.");
        }
        
        return Ok();
    }
    
    /// <summary>
    /// Delete cached upcoming contests
    /// </summary>
    /// <returns></returns>
    // DELETE <AdminController>/UpcomingContests
    [HttpDelete("[controller]/UpcomingContests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    public async Task<ActionResult> DeleteUpcomingContests() {
        var currentYear = SkyblockDate.Now.Year;
        var db = redis.GetDatabase();
        
        // Delete all upcoming contests
        await db.KeyDeleteAsync($"contests:{currentYear}");
        
        return Ok();
    }
}