using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.GuildService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers; 

[ServiceFilter(typeof(DiscordAuthFilter))]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IDiscordService _discordService;
    private readonly IGuildService _guildService;
    
    public AdminController(DataContext context, IMapper mapper, IDiscordService discordService, IGuildService guildService)
    {
        _context = context;
        _mapper = mapper;
        _discordService = discordService;
        _guildService = guildService;
    }
    
    // GET <EventController>/Admins
    [HttpGet("Admins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    public async Task<ActionResult<List<AccountWithPermsDto>>> GetAdmins() {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string) {
            return Unauthorized("Account not found.");
        }
        
        if (!account.Permissions.HasFlag(PermissionFlags.Admin)) {
            return Forbid("You do not have permission to do this!");
        }
        
        var members = await _context.Accounts
            .Where(a => a.Permissions > PermissionFlags.None)
            .AsNoTracking()
            .ToListAsync();
        
        return Ok(_mapper.Map<List<AccountWithPermsDto>>(members));
    }
    
    // POST <EventController>/Permissions/12793764936498429/17
    [HttpPost("[controller]/Permissions/{memberId:long}/{permission:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> PromoteMember(long memberId, ushort permission) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string) {
            return Unauthorized("Account not found.");
        }
        
        if (!account.Permissions.HasFlag(PermissionFlags.Admin)) {
            return Unauthorized("You do not have permission to do this!");
        }
        
        // Check that permission is valid
        if (!Enum.IsDefined(typeof(PermissionFlags), permission)) {
            return BadRequest("Invalid permission.");
        }

        var member = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == (ulong) memberId);
        
        if (member is null) {
            return NotFound("User not found.");
        }
        
        // Set permission
        member.Permissions = (PermissionFlags) permission;
        
        await _context.SaveChangesAsync();
        
        return Ok();
    }

    // DELETE <EventController>/Permissions/12793764936498429/17
    [HttpDelete("[controller]/Permissions/{memberId:long}/{permission:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult> DemoteMember(long memberId, ushort permission) {
        if (HttpContext.Items["Account"] is not EliteAccount account || HttpContext.Items["DiscordToken"] is not string) {
            return Unauthorized("Account not found.");
        }
        
        if (!account.Permissions.HasFlag(PermissionFlags.Admin)) {
            return Unauthorized("You do not have permission to do this!");
        }
        
        // Check that permission is valid
        if (!Enum.IsDefined(typeof(PermissionFlags), permission)) {
            return BadRequest("Invalid permission.");
        }
        
        var member = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == (ulong) memberId);
        
        if (member is null) {
            return NotFound("User not found.");
        }
        
        // Remove permission
        member.Permissions = PermissionFlags.None;
        
        await _context.SaveChangesAsync();
        
        return Ok();
    }
}