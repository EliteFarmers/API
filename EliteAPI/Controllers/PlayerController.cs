using Microsoft.AspNetCore.Mvc;
using EliteAPI.Services.ProfileService;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.AccountService;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class PlayerController(
    DataContext context, 
    IProfileService profileService, 
    IMapper mapper)
    : ControllerBase 
{
    /// <summary>
    /// Get player data by UUID or IGN
    /// </summary>
    /// <param name="playerUuidOrIgn"></param>
    /// <returns></returns>
    [HttpGet("{playerUuidOrIgn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public async Task<ActionResult<PlayerDataDto>> Get(string playerUuidOrIgn)
    {
        var playerData = await profileService.GetPlayerDataByUuidOrIgn(playerUuidOrIgn);
        if (playerData is null)
        {
            return NotFound("No player matching this UUID was found");
        }

        var mapped = mapper.Map<PlayerDataDto>(playerData);
        return Ok(mapped);
    }
    
    /// <summary>
    /// Get linked player data by Discord ID
    /// </summary>
    /// <param name="discordId"></param>
    /// <returns></returns>
    [HttpGet("{discordId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<ActionResult<LinkedAccountsDto>> GetLinkedAccounts(long discordId)
    {
        if (discordId <= 0)
        {
            return BadRequest("Invalid Discord ID");
        }
        
        var account = await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id == (ulong) discordId);
        
        if (account is null)
        {
            return NotFound("No linked account matching this Id was found");
        }
        
        var minecraftAccounts = account.MinecraftAccounts;
        var dto = new LinkedAccountsDto();
        
        foreach (var minecraftAccount in minecraftAccounts)
        {
            var data = await profileService.GetPlayerData(minecraftAccount.Id);
            if (data is null) continue;

            if (minecraftAccount.Selected) {
                dto.SelectedUuid = data.Uuid;
            }
            
            dto.Players.Add(mapper.Map<PlayerDataDto>(data));
        }
        
        return Ok(dto);
    }
}
