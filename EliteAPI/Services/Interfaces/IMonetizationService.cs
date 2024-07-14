using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Monetization;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IMonetizationService {
	Task UpdateProductAsync(ulong productId, UpdateProductDto updateProductDto);
	Task<List<UserEntitlement>> GetUserEntitlementsAsync(ulong userId);
	Task FetchUserEntitlementsAsync(ulong userId);
	Task<List<GuildEntitlement>> GetGuildEntitlementsAsync(ulong guildId);
	Task FetchGuildEntitlementsAsync(ulong guildId);
	Task<ActionResult> GrantTestEntitlementAsync(ulong targetId, ulong productId, EntitlementTarget target = EntitlementTarget.User);
	Task<ActionResult> RemoveTestEntitlementAsync(ulong targetId, ulong productId, EntitlementTarget target = EntitlementTarget.User);
}