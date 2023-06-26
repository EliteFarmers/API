using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities;
using EliteAPI.Services.ProfileService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IProfileService _profileService;
    private readonly IMapper _mapper;

    public AccountController(DataContext context, IProfileService profileService, IMapper mapper)
    {
        _context = context;
        _profileService = profileService;
        _mapper = mapper;
    }

    // GET api/<ValuesController>
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public ActionResult<Account> Get()
    {
        if (HttpContext.Items["Account"] is not Account result)
        {
            return Unauthorized("Account not found.");
        }

        var account = _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefault(a => a.Id.Equals(result.Id));

        return Ok(_mapper.Map<AccountDto>(account));
    }

    [HttpPost("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult<Account>> LinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not Account loggedInAccount)
        {
            return Unauthorized("Account not found.");
        }

        var account = await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(loggedInAccount.Id));

        if (account is null)
        {
            return Unauthorized("Account not found.");
        }

        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");

        // Check if the player has already linked their account
        if (account.MinecraftAccounts.Any(mc => mc.Id.Equals(id) || mc.Name.Equals(id)))
        {
            return BadRequest("Player has already linked this account.");
        }

        var playerData = await _profileService.GetPlayerDataByUuidOrIgn(id, true);

        if (playerData?.MinecraftAccount is null)
        {
            return BadRequest("No Minecraft account found for this player.");
        }

        var linkedDiscord = playerData.SocialMedia?.Discord;
        if (linkedDiscord is null)
        {
            return BadRequest("Player has not linked their discord.");
        }

        // Handle old Discord accounts with the discriminator (rip) 
        if (account.Discriminator is not null && !account.Discriminator.Equals("0"))
        {
            if (!linkedDiscord.Equals($"{account.Username}#{account.Discriminator}"))
            {
                return BadRequest("Player has a different account linked.");
            }
            // Success
            account.MinecraftAccounts.Add(playerData.MinecraftAccount);
            await _context.SaveChangesAsync();

            return Ok(account);
        }

        // Handle new Discord accounts without the discriminator
        if (!account.Username.Equals(linkedDiscord))
        {
            return BadRequest("Player has a different account linked.");
        }

        // Success
        account.MinecraftAccounts.Add(playerData.MinecraftAccount);
        await _context.SaveChangesAsync();
        
        return Accepted();
    }

    [HttpDelete("{playerUuidOrIgn}")]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public async Task<ActionResult<Account>> UnlinkAccount(string playerUuidOrIgn)
    {
        if (HttpContext.Items["Account"] is not Account linkedAccount)
        {
            return Unauthorized("Account not found.");
        }

        var account = await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id.Equals(linkedAccount.Id));

        if (account is null) return Unauthorized("Account not found.");
        
        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");
        var minecraftAccount = account.MinecraftAccounts.FirstOrDefault(mc => mc.Id.Equals(id) || mc.Name.Equals(id));

        // Check if the player has already linked their account
        if (minecraftAccount is null)
        {
            return BadRequest("Player has not linked this account.");
        }

        account.MinecraftAccounts.Remove(minecraftAccount);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
