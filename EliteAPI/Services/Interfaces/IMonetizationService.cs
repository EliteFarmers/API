using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Services.Interfaces;

public interface IMonetizationService {
	Task UpdateProductCategoryAsync(ulong productId, ProductCategory category);
	Task<List<UserEntitlement>> GetUserEntitlementsAsync(ulong userId);
	Task<List<GuildEntitlement>> GetGuildEntitlementsAsync(ulong guildId);
}