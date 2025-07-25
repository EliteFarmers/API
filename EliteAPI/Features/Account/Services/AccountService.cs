using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Models;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Account.Services;

[RegisterService<IAccountService>(LifeTime.Scoped)]
public class AccountService(
    DataContext context, 
    IMemberService memberService,
    IOptions<ConfigCooldownSettings> coolDowns,
    IOptions<FarmingItemsSettings> farmingItems) 
    : IAccountService 
{
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    private readonly FarmingItemsSettings _farmingItems = farmingItems.Value;
    
    public Task<EliteAccount?> GetAccountByIgnOrUuid(string ignOrUuid) {
        return ignOrUuid.Length == 32 ? GetAccountByMinecraftUuid(ignOrUuid) : GetAccountByIgn(ignOrUuid);
    }
    
    public async Task<EliteAccount?> GetAccount(ulong accountId) {
        return await context.Accounts
            .Include(a => a.MinecraftAccounts)
            .ThenInclude(a => a.Badges)
            .Include(a => a.UserSettings)
            .ThenInclude(a => a.WeightStyle)
            .Include(a => a.UserSettings)
            .ThenInclude(a => a.LeaderboardStyle)
            .Include(a => a.UserSettings)
            .ThenInclude(a => a.NameStyle)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<EliteAccount?> GetAccountByIgn(string ign)
    {
        var minecraftAccount = await context.MinecraftAccounts
            .AsNoTracking()
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

        if (playerData is null || playerData.LastUpdated.OlderThanSeconds(_coolDowns.HypixelPlayerDataLinkingCooldown)) {
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
            .Include(a => a.UserSettings)
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
        
        // Remove fortune settings for the account
        if (account.UserSettings.Fortune?.Accounts.ContainsKey(minecraftAccount.Id) is true)
        {
            account.UserSettings.Fortune.Accounts.Remove(minecraftAccount.Id);
        }
        
        context.Entry(account).State = EntityState.Modified;
        context.Entry(minecraftAccount).State = EntityState.Modified;
        context.Entry(account.UserSettings).State = EntityState.Modified;
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

    public async Task<ErrorOr<Success>> UpdateSettings(ulong discordId, UpdateUserSettingsDto settings) {
        var account = await GetAccount(discordId);

        if (account is null)
        {
            return Error.Unauthorized("Account not found.");
        }

        var changes = settings.Features;
        
        var entitlements = await context.ProductAccesses
            .Where(ue => ue.UserId == account.Id && !ue.Revoked)
            .Include(entitlement => entitlement.Product)
            .ToListAsync();

        if (settings.WeightStyleId is not null) {
            var validChange = entitlements.Any(ue => ue.IsActive && ue.HasWeightStyle(settings.WeightStyleId.Value));
            
            account.UserSettings.WeightStyleId = validChange ? settings.WeightStyleId : null;
            account.UserSettings.WeightStyle = null;
        }
        
        if (settings.LeaderboardStyleId is not null) {
            var validChange = entitlements.Any(ue => ue.IsActive && ue.HasWeightStyle(settings.LeaderboardStyleId.Value));
            
            account.UserSettings.LeaderboardStyleId = validChange ? settings.LeaderboardStyleId : null;
            account.UserSettings.LeaderboardStyle = null;
        }
        
        if (settings.NameStyleId is not null) {
            var validChange = entitlements.Any(ue => ue.IsActive && ue.HasWeightStyle(settings.NameStyleId.Value));
            
            account.UserSettings.NameStyleId = validChange ? settings.NameStyleId : null;
            account.UserSettings.NameStyle = null;
        }

        if (changes is not null)
        {
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
        }

        if (settings.Suffix is not null) {
            var validChange = entitlements.Any(ue => ue is { IsActive: true, Product.Features.CustomEmoji: true });

            if (settings.Suffix.IsNullOrEmpty()) {
                account.UserSettings.Suffix = null;
            } else {
                account.UserSettings.Suffix = validChange ? settings.Suffix : null;
            }
        }
        
        context.Accounts.Update(account);
        
        await context.SaveChangesAsync();

        return Result.Success;
    }
    
    public async Task<ErrorOr<Success>> UpdateFortuneSettings(ulong discordId, string playerUuid, string profileUuid, MemberFortuneSettingsDto settings) {
        var account = await GetAccount(discordId);

        if (account is null)
        {
            return Error.Unauthorized("Account not found.");
        }
        
        if (account.MinecraftAccounts.All(mc => mc.Id != playerUuid)) {
            return Error.Validation($"Minecraft account with ID {playerUuid} not linked to {discordId}.");
        }
        
        var existing = account.UserSettings.Fortune ?? new FortuneSettingsDto();

        if (!existing.Accounts.ContainsKey(playerUuid))
        {
            existing.Accounts[playerUuid] = new Dictionary<string, MemberFortuneSettingsDto>();
        }
        
        if (settings.CommunityCenter is < 0 or > 10) {
            return Error.Validation($"Community Center level must be between 0 and 10.");
        }

        if (settings.Strength is < 0 or > 5000) {
            return Error.Validation($"Strength must be between 0 and 5000.");
        }
        
        if (settings.Attributes.Any(kvp => kvp.Value < 0 || kvp.Value > 500 || !_farmingItems.ShardIds.Contains(kvp.Key)))
        {
            return Error.Validation("Attribute values must be between 0 and 500 and must be valid shards.");
        }
        
        if (settings.Exported.Any(kvp => FormatUtils.GetCropFromItemId(kvp.Key) is null))
        {
            // Ensure all exported crops are valid crop IDs
            return Error.Validation("Exported crops must be valid crop IDs.");
        }
        
        existing.Accounts[playerUuid][profileUuid] = settings;
        account.UserSettings.Fortune = existing;
        
        context.Entry(account.UserSettings).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return Result.Success;
    }
}
