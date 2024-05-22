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
[Authorize(ApiUserRoles.Moderator)]
public class AdminController(
    DataContext context,
    IConnectionMultiplexer redis,
    UserManager<ApiUser> userManager) 
    : ControllerBase
{
    
    /// <summary>
    /// Get list of members with roles
    /// </summary>
    /// <returns></returns>
    [HttpGet("Admins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    public async Task<ActionResult<List<AccountWithPermsDto>>> GetAdmins() {
        var members = new List<AccountWithPermsDto>();

        await LoadUsers(ApiUserRoles.Admin);
        await LoadUsers(ApiUserRoles.Moderator);
        await LoadUsers(ApiUserRoles.Support);
        await LoadUsers(ApiUserRoles.Wiki);
        
        return members;

        // Inefficient, but it works for now as the number of admins is low
        async Task LoadUsers(string role) {
            var users = await userManager.GetUsersInRoleAsync(ApiUserRoles.Admin);

            foreach (var user in users) {
                if (members?.Find(m => m.Id.Equals(user.Id)) is {} existingMember) {
                    existingMember.Roles.Add(role);
                    continue;
                }
            
                await context.Entry(user).Reference(m => m.Account).LoadAsync();
            
                var member = new AccountWithPermsDto {
                    Id = user.Id,
                    DisplayName = user.Account.DisplayName,
                    Username = user.UserName ?? user.Account.Username,
                    Avatar = user.Account.Avatar,
                    Discriminator = user.Account.Discriminator,
                    Roles = [ role ]
                };
            
                members?.Add(member);
            }
        } 
    }
    
    // POST <AdminController>/Permissions/12793764936498429/17
    /// <summary>
    /// Add member permissions
    /// </summary>
    /// <param name="memberId"></param>
    /// <param name="permission"></param>
    /// <returns></returns>
    [Authorize(ApiUserRoles.Admin)]
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
    [Authorize(ApiUserRoles.Admin)]
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
    [Authorize(ApiUserRoles.Admin)]
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
    [Authorize(ApiUserRoles.Admin)]
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