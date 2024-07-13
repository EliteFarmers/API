using Asp.Versioning;
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

[Authorize(ApiUserPolicies.Moderator)]
[ApiController, ApiVersion(1.0)]
[Route("[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public class AdminController(
    DataContext context,
    IConnectionMultiplexer redis,
    UserManager<ApiUser> userManager) 
    : ControllerBase
{
    
    /// <summary>
    /// Get admin list
    /// </summary>
    /// <response code="200">List of admins</response>
    [HttpGet]
    [Route("[controller]s")]
    [Route("/v{version:apiVersion}/[controller]s")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    public async Task<ActionResult<List<AccountWithPermsDto>>> GetAdmins() {
        // I'm sure this query can be optimized further.
        // Right now it's not expected to handle a large amount of users.
        var users = from user in context.Users
            join account in context.Accounts on user.AccountId equals account.Id
            join userRole in context.UserRoles on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in context.Roles on userRole.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            where role == null || role.Name != ApiUserPolicies.User
            group new { user, account, role } by new { user.Id, user.UserName }
            into g
            select new AccountWithPermsDto {
                Id = g.Key.Id,
                DisplayName = g.Max(x => x.account.DisplayName),
                Username = g.Key.UserName ?? g.Max(x => x.account.Username),
                Avatar = g.Max(x => x.account.Avatar),
                Discriminator = g.Max(x => x.account.Discriminator),
                Roles = g.Where(x => x.role != null).Select(x => x.role.Name).ToList()
            };
        
        return await users.AsNoTracking().AsSplitQuery().ToListAsync();
    }
    
    /// <summary>
    /// Get list of roles
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/roles")]
    [Route("/v{version:apiVersion}/roles")]
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
    [Authorize(ApiUserPolicies.Admin)]
    [HttpPost("user/{userId}/Roles/{role}")]
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
    [Authorize(ApiUserPolicies.Admin)]
    [HttpDelete("user/{userId}/Roles/{role}")]
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
    [Authorize(ApiUserPolicies.Admin)]
    [HttpDelete("upcomingcontests")]
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