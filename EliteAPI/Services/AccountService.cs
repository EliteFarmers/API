using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services;

public class AccountService(DataContext context, IMemberService memberService) : IAccountService {
    public Task<EliteAccount?> GetAccountByIgnOrUuid(string ignOrUuid) {
        return ignOrUuid.Length == 32 ? GetAccountByMinecraftUuid(ignOrUuid) : GetAccountByIgn(ignOrUuid);
    }
    
    public async Task<EliteAccount?> GetAccount(ulong accountId) {
        return await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .ThenInclude(a => a.Badges)
            .Include(a => a.UserSettings)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<EliteAccount?> GetAccountByIgn(string ign)
    {
        var minecraftAccount = await context.MinecraftAccounts
            .Include(mc => mc.Badges)
            .Where(mc => mc.Name == ign)
            .FirstOrDefaultAsync();
        
        if (minecraftAccount?.AccountId is null) return null;

        return await GetAccount(minecraftAccount.AccountId ?? 0);
    }

    public async Task<EliteAccount?> GetAccountByMinecraftUuid(string uuid)
    {
        var minecraftAccount = await context.MinecraftAccounts
            .Include(mc => mc.Badges)
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
        if (account.MinecraftAccounts.Any(mc => mc.Id.Equals(id) || mc.Name.ToLower().Equals(id.ToLower())))
        {
            return new BadRequestObjectResult("You have already linked this account.");
        }

        var playerData = await context.PlayerData
            .Include(pd => pd.MinecraftAccount)
            .Include(pd => pd.SocialMedia)
            .Where(pd => pd.MinecraftAccount!.Id.Equals(id) || pd.MinecraftAccount.Name == id)
            .FirstOrDefaultAsync();

        if (playerData is not null && playerData.LastUpdated.OlderThanSeconds(120)) {
            await memberService.RefreshPlayerData(id);
            
            playerData = await context.PlayerData
                .Include(pd => pd.MinecraftAccount)
                .Include(pd => pd.SocialMedia)
                .Where(pd => pd.MinecraftAccount!.Id.Equals(id) || pd.MinecraftAccount.Name == id)
                .FirstOrDefaultAsync();
        }
        
        if (playerData?.MinecraftAccount is null)
        {
            return new BadRequestObjectResult("No Minecraft account found. Please ensure you entered the correct player name or try looking up their stats first.");
        }

        // Remove "#0000" because some other (bad) applications require the discriminator in Hypixel to be zeros
        var linkedDiscord = playerData.SocialMedia.Discord?.Replace("#0000", "");
        if (linkedDiscord is null)
        {
            return new BadRequestObjectResult("You have not linked a Discord account in the Hypixel social menu. Do that first and try again.");
        }

        // Handle old Discord accounts with the discriminator (rip) 
        if (account.Discriminator is not null && !account.Discriminator.Equals("0")) {
            var tag = $"{account.Username}#{account.Discriminator}";
            if (!linkedDiscord.Equals($"{account.Username}#{account.Discriminator}"))
            {
                return new BadRequestObjectResult($"`{id}` has the account `{linkedDiscord}` linked in Hypixel.\nPlease change this to `{tag}` within Hypixel or ensure you entered the correct player name.");
            }
        } 
        else if (!account.Username.ToLower().Equals(linkedDiscord.ToLower())) // Handle new Discord accounts without the discriminator
        {
            return new BadRequestObjectResult($"`{id}` has the account `{linkedDiscord}` linked in Hypixel.\nPlease change this to `{account.Username}` within Hypixel or ensure you entered the correct player name.");
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
        context.MinecraftAccounts.Update(playerData.MinecraftAccount);
        
        await context.SaveChangesAsync();
        
        return new AcceptedResult();
    }

    public async Task<ActionResult> UnlinkAccount(ulong discordId, string playerUuidOrIgn) {
        var account = await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id == discordId);
        
        if (account is null) return new UnauthorizedObjectResult("Account not found.");
        
        // Remove dashes from id
        var id = playerUuidOrIgn.Replace("-", "");
        var minecraftAccount = account.MinecraftAccounts
            .FirstOrDefault(mc => mc.Id.Equals(id) || mc.Name.ToLower().Equals(id.ToLower()));

        // Check if the player has already linked their account
        if (minecraftAccount is null)
        {
            return new BadRequestObjectResult("You have not linked this account.");
        }
        
        // Remove the badges from the user that are tied to the account
        context.UserBadges.RemoveRange(minecraftAccount.Badges.Where(x => x.Badge.TieToAccount));
        
        // Reset the account id
        minecraftAccount.AccountId = null;
        minecraftAccount.EliteAccount = null;
        minecraftAccount.Selected = false;
        account.MinecraftAccounts.Remove(minecraftAccount);
        
        context.Entry(account).State = EntityState.Modified;
        context.Entry(minecraftAccount).State = EntityState.Modified;
        await context.SaveChangesAsync();

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
        var newSelectedAccount = mcAccounts.FirstOrDefault(mc => mc.Id.Equals(playerUuidOrIgn) || mc.Name.ToLower().Equals(playerUuidOrIgn.ToLower()));
        
        if (newSelectedAccount is null)
        {
            return new BadRequestObjectResult("Minecraft account not found for this player.");
        }
        
        if (selectedAccount is not null)
        {
            selectedAccount.Selected = false;
            context.MinecraftAccounts.Update(selectedAccount);
        }
        
        newSelectedAccount.Selected = true;
        context.MinecraftAccounts.Update(newSelectedAccount);
        
        await context.SaveChangesAsync();
        
        return new AcceptedResult();
    }

    public async Task<ActionResult> UpdateSettings(ulong discordId, UpdateUserSettingsDto settings) {
        var account = await GetAccount(discordId);

        if (account is null)
        {
            return new UnauthorizedObjectResult("Account not found.");
        }

        if (settings.Features is null) {
            return new OkResult();
        }

        var changes = settings.Features;

        var entitlements = await context.UserEntitlements
            .Where(ue => ue.AccountId == account.Id && !ue.Deleted
                    && (ue.StartDate == null || ue.StartDate <= DateTimeOffset.UtcNow) 
                    && (ue.EndDate == null || ue.EndDate >= DateTimeOffset.UtcNow))
            .Include(entitlement => entitlement.Product)
            .ToListAsync();

        if (settings.WeightStyleId is not null) {
            account.UserSettings.WeightStyleId =
                entitlements.Any(ue => ue.Active && ue.HasWeightStyle(settings.WeightStyleId.Value))
                    ? settings.WeightStyleId
                    : null; // Clear the weight style if not valid (also allows for resetting the weight style)
        }
        
        if (changes.WeightStyleOverride is true 
            && entitlements.Any(ue => ue.Product.Features.WeightStyleOverride))
        {
            account.UserSettings.Features.WeightStyleOverride = true;
        } 
        else if (changes.WeightStyleOverride is false)
        {
            account.UserSettings.Features.WeightStyleOverride = false;
        }
        
        if (changes.MoreInfoDefault is true 
            && entitlements.Any(ue => ue.Product.Features.MoreInfoDefault))
        {
            account.UserSettings.Features.MoreInfoDefault = true;
        } 
        else if (changes.MoreInfoDefault is false)
        {
            account.UserSettings.Features.MoreInfoDefault = false;
        }
        
        if (changes.HideShopPromotions is true 
            && entitlements.Any(ue => ue.Product.Features.HideShopPromotions))
        {
            account.UserSettings.Features.HideShopPromotions = true;
        }
        else if (changes.HideShopPromotions is false)
        {
            account.UserSettings.Features.HideShopPromotions = false;
        }

        if (changes.EmbedColor is not null) {
            account.UserSettings.Features.EmbedColor = 
                entitlements.Any(ue => ue.Product.Features.EmbedColors?.Contains(changes.EmbedColor) is true)
                    ? changes.EmbedColor
                    : null; // Clear the embed color if not valid (also allows for resetting the embed color)
        }
        
        context.Accounts.Update(account);
        
        await context.SaveChangesAsync();
        
        return new OkResult();
    }
}
