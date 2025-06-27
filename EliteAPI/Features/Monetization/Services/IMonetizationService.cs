using EliteAPI.Features.Monetization.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Monetization;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Monetization.Services;

public interface IMonetizationService {
	Task UpdateProductAsync(ulong productId, EditProductDto editProductDto);
	Task<List<ProductAccess>> GetEntitlementsAsync(ulong entityId);
	Task FetchUserEntitlementsAsync(ulong userId);
	Task SyncDiscordEntitlementsAsync(ulong entityId, bool isGuild);
	Task FetchGuildEntitlementsAsync(ulong guildId);
	Task GrantProductAccessAsync(ulong userId, ulong productId);
	Task<ActionResult> GrantTestEntitlementAsync(ulong targetId, ulong productId, EntitlementTarget target = EntitlementTarget.User);
	Task<ActionResult> RemoveTestEntitlementAsync(ulong targetId, ulong entitlementId, EntitlementTarget target = EntitlementTarget.User);
}