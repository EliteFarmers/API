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
public class PlayerController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IProfileService _profileService;
    private readonly IMapper _mapper;

    public PlayerController(DataContext context, IProfileService profileService, IMapper mapper)
    {
        _context = context;
        _profileService = profileService;
        _mapper = mapper;
    }

    // GET <ProfileController>
    [HttpGet("{playerUuidOrIgn}")]
    public async Task<ActionResult<PlayerDataDto>> Get(string playerUuidOrIgn)
    {
        var playerData = await _profileService.GetPlayerDataByUuidOrIgn(playerUuidOrIgn);
        if (playerData is null)
        {
            return NotFound("No player matching this UUID was found");
        }

        var mapped = _mapper.Map<PlayerDataDto>(playerData);
        return Ok(mapped);
    }
    
    // POST <ProfileController>
    [HttpGet("{discordId:long}")]
    public async Task<ActionResult<LinkedAccountsDto>> GetLinkedAccounts(long discordId)
    {
        if (discordId <= 0)
        {
            return BadRequest("Invalid Discord ID");
        }
        
        var account = await _context.Accounts
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
            var data = await _profileService.GetPlayerData(minecraftAccount.Id);
            if (data is null) continue;

            if (minecraftAccount.Selected) {
                dto.SelectedUuid = data.Uuid;
            }
            
            dto.Players.Add(_mapper.Map<PlayerDataDto>(data));
        }
        
        return Ok(dto);
    }
}
