using EliteAPI.Data;
using EliteAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.AccountService;

public class AccountService : IAccountService
{
    private readonly DataContext _context;
    public AccountService(DataContext context)
    {
        _context = context;
    }

    public Task<AccountEntity?> GetAccountByIgnOrUuid(string ignOrUuid) {
        return ignOrUuid.Length == 32 ? GetAccountByMinecraftUuid(ignOrUuid) : GetAccountByIgn(ignOrUuid);
    }
    
    public async Task<AccountEntity?> GetAccount(ulong accountId) {
        return await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<AccountEntity?> GetAccountByIgn(string ign)
    {
        var minecraftAccount = await _context.MinecraftAccounts
            .FirstOrDefaultAsync(mc => mc.Name.Equals(ign));
        
        if (minecraftAccount?.AccountId is null) return null;

        return await GetAccount(minecraftAccount.AccountId ?? 0);
    }

    public async Task<AccountEntity?> GetAccountByMinecraftUuid(string uuid)
    {
        var minecraftAccount = await _context.MinecraftAccounts
            .FirstOrDefaultAsync(mc => mc.Id.Equals(uuid));
        
        if (minecraftAccount?.AccountId is null) return null;

        return await GetAccount(minecraftAccount.AccountId ?? 0);
    }
    
    public async Task<ActionResult> LinkAccount(ulong discordId, string playerUuidOrIgn) {
        var account = await GetAccount(discordId);
        
        if (account is null) {
            return new UnauthorizedObjectResult("Account not found.");
        }

        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");

        // Check if the player has already linked this account
        if (account.MinecraftAccounts.Any(mc => mc.Id.Equals(id) || mc.Name.Equals(id)))
        {
            return new BadRequestObjectResult("You have already linked this account.");
        }

        var playerData = await _context.PlayerData
            .Include(pd => pd.MinecraftAccount)
            .Include(pd => pd.SocialMedia)
            .FirstOrDefaultAsync(pd => pd.MinecraftAccount!.Id.Equals(id) || pd.MinecraftAccount.Name.Equals(id));

        if (playerData?.MinecraftAccount is null)
        {
            return new BadRequestObjectResult("No Minecraft account found. Please ensure you entered the correct player name or try looking up their stats first.");
        }

        var linkedDiscord = playerData.SocialMedia.Discord;
        if (linkedDiscord is null)
        {
            return new BadRequestObjectResult("You have not linked a Discord account in the Hypixel social menu.");
        }

        // Handle old Discord accounts with the discriminator (rip) 
        if (account.Discriminator is not null && !account.Discriminator.Equals("0")) {
            var tag = $"{account.Username}#{account.Discriminator}";
            if (!linkedDiscord.Equals($"{account.Username}#{account.Discriminator}"))
            {
                return new BadRequestObjectResult($"`{id}` has the account `{linkedDiscord}` linked in Hypixel.\nPlease change this to `{tag}` instead or ensure you entered the correct player name.");
            }
        } 
        else if (!account.Username.Equals(linkedDiscord)) // Handle new Discord accounts without the discriminator
        {
            return new BadRequestObjectResult($"`{id}` has the account `{linkedDiscord}` linked in Hypixel.\nPlease change this to `{account.Username}` instead or ensure you entered the correct player name.");
        }

        // Success
        account.MinecraftAccounts.Add(playerData.MinecraftAccount);
        
        // Select the account if it's the only one
        if (account.MinecraftAccounts.Count == 1)
        {
            playerData.MinecraftAccount.Selected = true;
        }
        
        // Set the account id
        playerData.MinecraftAccount.AccountId = account.Id;
        _context.MinecraftAccounts.Update(playerData.MinecraftAccount);
        
        await _context.SaveChangesAsync();
        
        return new AcceptedResult();
    }

    public async Task<ActionResult> UnlinkAccount(ulong discordId, string playerUuidOrIgn) {
        var account = await GetAccount(discordId);
        
        if (account is null) return new UnauthorizedObjectResult("Account not found.");
        
        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");
        var minecraftAccount = account.MinecraftAccounts.FirstOrDefault(mc => mc.Id.Equals(id) || mc.Name.Equals(id));

        // Check if the player has already linked their account
        if (minecraftAccount is null)
        {
            return new BadRequestObjectResult("You have not linked this account.");
        }
        
        // Reset the account id
        minecraftAccount.AccountId = null;
        minecraftAccount.Selected = false;
        account.MinecraftAccounts.Remove(minecraftAccount);
        
        await _context.SaveChangesAsync();

        return new NoContentResult();
    }

    public async Task<ActionResult> MakePrimaryAccount(ulong discordId, string playerUuidOrIgn) {
        var account = await GetAccount(discordId);

        if (account is null)
        {
            return new UnauthorizedObjectResult("Account not found.");
        }

        var mcAccounts = account.MinecraftAccounts;
        var selectedAccount = mcAccounts.FirstOrDefault(mc => mc.Selected);
        var newSelectedAccount = mcAccounts.FirstOrDefault(mc => mc.Id.Equals(playerUuidOrIgn) || mc.Name.Equals(playerUuidOrIgn));
        
        if (newSelectedAccount is null)
        {
            return new BadRequestObjectResult("Minecraft account not found for this player.");
        }
        
        if (selectedAccount is not null)
        {
            selectedAccount.Selected = false;
            _context.MinecraftAccounts.Update(selectedAccount);
        }
        
        newSelectedAccount.Selected = true;
        _context.MinecraftAccounts.Update(newSelectedAccount);
        
        await _context.SaveChangesAsync();
        
        return new AcceptedResult();
    }
}
