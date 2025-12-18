using System.Collections.Concurrent;
using System.Security.Claims;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Account.Services;

[RegisterService<IAccountService>(LifeTime.Scoped)]
public class AccountService(
	DataContext context,
	IMemberService memberService,
	IMojangService mojangService,
	IServiceScopeFactory scopeFactory,
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

	public async Task<EliteAccount?> GetAccountByIgn(string ign) {
		var minecraftAccount = await context.MinecraftAccounts
			.AsNoTracking()
			.Include(mc => mc.Badges)
			.Where(mc => mc.Name == ign)
			.FirstOrDefaultAsync();

		if (minecraftAccount?.AccountId is null) return null;

		return await GetAccount(minecraftAccount.AccountId ?? 0);
	}

	public async Task<EliteAccount?> GetAccountByMinecraftUuid(string uuid) {
		var minecraftAccount = await context.MinecraftAccounts
			.Include(mc => mc.Badges)
			.FirstOrDefaultAsync(mc => mc.Id.Equals(uuid));

		if (minecraftAccount?.AccountId is null) return null;

		return await GetAccount(minecraftAccount.AccountId ?? 0);
	}

	public async Task<ErrorOr<Success>> LinkAccount(ulong discordId, string playerUuidOrIgn) {
		var account = await GetAccount(discordId);

		if (account is null) return Error.Unauthorized(description: "Account not found.");

		// Remove dashes from id
		var id = playerUuidOrIgn.Replace("-", "");

		// Check if the player has already linked this account
		if (account.MinecraftAccounts.Any(mc => mc.Id.Equals(id) || mc.Name.ToLower().Equals(id.ToLower())))
			return Error.Failure(description: "You have already linked this account.");

		var playerData = await context.PlayerData
			.Include(pd => pd.MinecraftAccount)
			.Include(pd => pd.SocialMedia)
			.Where(pd => pd.MinecraftAccount!.Id.Equals(id) || pd.MinecraftAccount.Name == id)
			.FirstOrDefaultAsync();

		if (playerData is null ||
		    playerData.LastUpdated.OlderThanSeconds(_coolDowns.HypixelPlayerDataLinkingCooldown)) {
			await memberService.RefreshPlayerData(id);

			playerData = await context.PlayerData
				.Include(pd => pd.MinecraftAccount)
				.Include(pd => pd.SocialMedia)
				.Where(pd => pd.MinecraftAccount!.Id.Equals(id) || pd.MinecraftAccount.Name == id)
				.FirstOrDefaultAsync();
		}

		if (playerData?.MinecraftAccount is null)
			return Error.Failure(
				description:
				"No Minecraft account found. Please ensure you entered the correct player name or try looking up their stats first.");

		// Remove "#0000" because some other (bad) applications require the discriminator in Hypixel to be zeros
		var linkedDiscord = playerData.SocialMedia.Discord?.Replace("#0000", "");
		if (linkedDiscord is null)
			return Error.Failure(
				description:
				"You have not linked a Discord account in the Hypixel social menu. Do that first and try again.");

		// Handle old Discord accounts with the discriminator (rip) 
		if (account.Discriminator is not null && !account.Discriminator.Equals("0")) {
			var tag = $"{account.Username}#{account.Discriminator}";
			if (!linkedDiscord.Equals($"{account.Username}#{account.Discriminator}"))
				return Error.Failure(
					description:
					$"`{id}` has the account `{linkedDiscord}` linked in Hypixel.\nPlease change this to `{tag}` within Hypixel or ensure you entered the correct player name.");
		}
		else if (!account.Username.ToLower()
			         .Equals(linkedDiscord.ToLower())) // Handle new Discord accounts without the discriminator
		{
			return Error.Failure(
				description:
				$"`{id}` has the account `{linkedDiscord}` linked in Hypixel.\nPlease change this to `{account.Username}` within Hypixel or ensure you entered the correct player name.");
		}

		// Select the account if it's the only one
		if (account.MinecraftAccounts.Count == 0) playerData.MinecraftAccount.Selected = true;

		// Set the account id
		playerData.MinecraftAccount.AccountId = account.Id;
		context.MinecraftAccounts.Update(playerData.MinecraftAccount);

		await context.SaveChangesAsync();

		return Result.Success;
	}

	public async Task<ErrorOr<Success>> UnlinkAccount(ulong discordId, string playerUuidOrIgn) {
		var account = await context.Accounts
			.Include(a => a.MinecraftAccounts)
			.Include(a => a.UserSettings)
			.FirstOrDefaultAsync(a => a.Id == discordId);

		if (account is null) return Error.Unauthorized(description: "Account not found.");

		// Remove dashes from id
		var id = playerUuidOrIgn.Replace("-", "");
		var minecraftAccount = account.MinecraftAccounts
			.FirstOrDefault(mc => mc.Id.Equals(id) || mc.Name.ToLower().Equals(id.ToLower()));

		// Check if the player has already linked their account
		if (minecraftAccount is null) return Error.Failure(description: "You have not linked this account.");

		// Remove the badges from the user that are tied to the account
		context.UserBadges.RemoveRange(minecraftAccount.Badges.Where(x => x.Badge.TieToAccount));

		if (minecraftAccount.Selected && account.MinecraftAccounts.Count > 1) {
			// If the account is selected and there are other accounts, select another one
			var newSelectedAccount = account.MinecraftAccounts.FirstOrDefault(mc => mc.Id != minecraftAccount.Id);
			if (newSelectedAccount is not null) newSelectedAccount.Selected = true;
		}

		// Reset the account id
		minecraftAccount.AccountId = null;
		minecraftAccount.EliteAccount = null;
		minecraftAccount.Selected = false;
		account.MinecraftAccounts.Remove(minecraftAccount);

		// Remove fortune settings for the account
		if (account.UserSettings.Fortune?.Accounts.ContainsKey(minecraftAccount.Id) is true) {
			account.UserSettings.Fortune.Accounts.Remove(minecraftAccount.Id);
			context.Entry(account.UserSettings).State = EntityState.Modified;
		}

		await context.SaveChangesAsync();

		return Result.Success;
	}

	public async Task<ErrorOr<Success>> MakePrimaryAccount(ulong discordId, string playerUuidOrIgn) {
		var account = await GetAccount(discordId);

		if (account is null) return Error.Unauthorized(description: "Account not found.");

		var mcAccounts = account.MinecraftAccounts;
		var selectedAccount = mcAccounts.FirstOrDefault(mc => mc.Selected);
		var newSelectedAccount = mcAccounts.FirstOrDefault(mc =>
			mc.Id.Equals(playerUuidOrIgn) || mc.Name.ToLower().Equals(playerUuidOrIgn.ToLower()));

		if (newSelectedAccount is null)
			return Error.Failure(description: "Minecraft account not found for this player.");

		if (selectedAccount is not null) {
			selectedAccount.Selected = false;
			context.MinecraftAccounts.Update(selectedAccount);
		}

		newSelectedAccount.Selected = true;
		context.MinecraftAccounts.Update(newSelectedAccount);

		await context.SaveChangesAsync();

		return Result.Success;
	}

	public async Task<ErrorOr<Success>> UpdateSettings(ulong discordId, UpdateUserSettingsDto settings) {
		var account = await GetAccount(discordId);

		if (account is null) return Error.Unauthorized(description: "Account not found.");

		var changes = settings.Features;

		var entitlements = await context.ProductAccesses
			.Where(ue => ue.UserId == account.Id)
			.WhereActive()
			.Include(entitlement => entitlement.Product)
			.ThenInclude(p => p.WeightStyles)
			.AsNoTracking()
			.ToListAsync();

		account.UserSettings.Features.Flags = entitlements
			.Where(e => e.Product.Features.Flags.Length > 0)
			.SelectMany(e => e.Product.Features.Flags)
			.Distinct()
			.ToArray();

		if (settings.WeightStyleId is not null) {
			var validChange = entitlements.Any(ue => ue.HasWeightStyle(settings.WeightStyleId.Value));

			account.UserSettings.WeightStyleId = validChange ? settings.WeightStyleId : null;
			account.UserSettings.WeightStyle = null;
		}

		if (settings.LeaderboardStyleId is not null) {
			var validChange = entitlements.Any(ue => ue.HasLeaderboardStyle(settings.LeaderboardStyleId.Value));

			account.UserSettings.LeaderboardStyleId = validChange ? settings.LeaderboardStyleId : null;
			account.UserSettings.LeaderboardStyle = null;
		}

		if (settings.NameStyleId is not null) {
			var validChange = entitlements.Any(ue => ue.HasNameStyle(settings.NameStyleId.Value));

			account.UserSettings.NameStyleId = validChange ? settings.NameStyleId : null;
			account.UserSettings.NameStyle = null;
		}

		if (changes is not null) {
			if (changes.WeightStyleOverride is true
			    && entitlements.Any(ue => ue.Product.Features.WeightStyleOverride))
				account.UserSettings.Features.WeightStyleOverride = true;
			else if (changes.WeightStyleOverride is false) account.UserSettings.Features.WeightStyleOverride = false;

			if (changes.MoreInfoDefault is true
			    && entitlements.Any(ue => ue.Product.Features.MoreInfoDefault))
				account.UserSettings.Features.MoreInfoDefault = true;
			else if (changes.MoreInfoDefault is false) account.UserSettings.Features.MoreInfoDefault = false;

			if (changes.HideShopPromotions is true
			    && entitlements.Any(ue => ue.Product.Features.HideShopPromotions))
				account.UserSettings.Features.HideShopPromotions = true;
			else if (changes.HideShopPromotions is false) account.UserSettings.Features.HideShopPromotions = false;

			if (changes.EmbedColor is not null)
				account.UserSettings.Features.EmbedColor =
					entitlements.Any(ue => ue.Product.Features.EmbedColors?.Contains(changes.EmbedColor) is true)
						? changes.EmbedColor
						: null; // Clear the embed color if not valid (also allows for resetting the embed color)
		}

		if (settings.Suffix is not null) {
			var validChange = entitlements.Any(ue => ue is { IsActive: true, Product.Features.CustomEmoji: true });

			if (settings.Suffix.IsNullOrEmpty())
				account.UserSettings.Suffix = null;
			else
				account.UserSettings.Suffix = validChange ? settings.Suffix : null;
		}

		context.Accounts.Update(account);

		await context.SaveChangesAsync();

		return Result.Success;
	}

	public async Task<ErrorOr<Success>> UpdateFortuneSettings(ulong discordId, string playerUuid, string profileUuid,
		MemberFortuneSettingsDto settings) {
		var account = await GetAccount(discordId);

		if (account is null) return Error.Unauthorized(description: "Account not found.");

		if (account.MinecraftAccounts.All(mc => mc.Id != playerUuid))
			return Error.Validation(description: $"Minecraft account with ID {playerUuid} not linked to {discordId}.");

		var existing = account.UserSettings.Fortune ?? new FortuneSettingsDto();

		if (!existing.Accounts.ContainsKey(playerUuid))
			existing.Accounts[playerUuid] = new Dictionary<string, MemberFortuneSettingsDto>();

		if (settings.CommunityCenter is < 0 or > 10)
			return Error.Validation(description: "Community Center level must be between 0 and 10.");

		if (settings.Strength is < 0 or > 5000)
			return Error.Validation(description: "Strength must be between 0 and 5000.");

		if (settings.Attributes.Any(kvp =>
			    kvp.Value < 0 || kvp.Value > 500 || !_farmingItems.ShardIds.Contains(kvp.Key)))
			return Error.Validation(
				description: "Attribute values must be between 0 and 500 and must be valid shards.");

		if (settings.Chips.Any(kvp =>
			    kvp.Value < 0 || kvp.Value > 20 || !_farmingItems.ChipIds.Contains(kvp.Key)))
			return Error.Validation(
				description: "Chip values must be between 0 and 20 and must be valid chips.");

		existing.Accounts[playerUuid][profileUuid] = settings;
		account.UserSettings.Fortune = existing;

		context.Entry(account.UserSettings).State = EntityState.Modified;
		await context.SaveChangesAsync();

		return Result.Success;
	}

	public async Task<AccountMetaDto?> GetAccountMeta(string uuid) {
		return await context.MinecraftAccounts.AsNoTracking()
			.Include(account => account.EliteAccount)
			.SelectMetaDto()
			.FirstOrDefaultAsync();
	}

	public async Task<Dictionary<string, AccountMetaDto?>> GetAccountMeta(List<string> uuids) {
		var dict = await context.MinecraftAccounts.AsNoTracking()
			.Include(account => account.EliteAccount)
			.Where(account => uuids.Contains(account.Id))
			.SelectMetaDto()
			.ToDictionaryAsync(account => account.Id, account => (AccountMetaDto?) account);
		
		var concurrentDict = new ConcurrentDictionary<string, AccountMetaDto?>(dict);

		await Parallel.ForEachAsync(uuids, new ParallelOptions() { MaxDegreeOfParallelism = 10 },
			async (string uuid, CancellationToken token) => {
				using var scope = scopeFactory.CreateScope();
				var mojang = scope.ServiceProvider.GetRequiredService<IMojangService>();
				if (concurrentDict.ContainsKey(uuid)) return;

				var account = await mojang.GetMinecraftAccountByUuid(uuid);

				if (account is null) {
					concurrentDict.TryAdd(uuid, null);
					return;
				}

				concurrentDict.TryAdd(uuid, new AccountMetaDto() {
					Id = account.Id,
					Name = account.Name,
					FormattedName = account.Name,
				});
			});

		return concurrentDict.ToDictionary(account => account.Key, account => account.Value);
	}

	public async Task<bool> OwnsMinecraftAccount(ClaimsPrincipal user, string playerUuidOrIgn, string roleOverride = ApiUserPolicies.Admin) {
		if (user.IsInRole(roleOverride) || user.IsInRole(ApiUserPolicies.Admin)) return true;
		
		var discordId = user.GetDiscordId();
		if (discordId is null) return false;
		
		return await context.MinecraftAccounts
			.AnyAsync(mc =>
				mc.AccountId == discordId &&
				(mc.Id.Equals(playerUuidOrIgn) || mc.Name.ToLower().Equals(playerUuidOrIgn.ToLower())));
	}
}