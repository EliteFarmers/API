using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.Services;

[RegisterService<IBadgeService>(LifeTime.Scoped)]
public class BadgeService(DataContext context, IMojangService mojangService) : IBadgeService {
	public async Task<Badge?> GetBadgeById(int id) {
		return await context.Badges.FindAsync(id);
	}

	public async Task<ActionResult> AddBadgeToUser(string playerUuid, int badgeId) {
		var badge = await context.Badges.FindAsync(badgeId);

		if (badge is null) return new NotFoundObjectResult("Badge not found");

		var player = await mojangService.GetMinecraftAccountByUuidOrIgn(playerUuid);

		if (player is null) return new NotFoundObjectResult("Player not found");

		if (player.Badges.Any(x => x.BadgeId == badgeId))
			return new BadRequestObjectResult("Player already has this badge");

		// Remove the badge from the user's other accounts if it is tied to the account
		if (badge.TieToAccount) {
			var account = await context.MinecraftAccounts
				.Where(m => m.EliteAccount != null)
				.Include(a => a.EliteAccount)
				.ThenInclude(e => e!.MinecraftAccounts)
				.ThenInclude(a => a.Badges)
				.AsSplitQuery()
				.FirstOrDefaultAsync(a => a.Id == playerUuid);

			if (account?.EliteAccount is null)
				return new BadRequestObjectResult("This badge requires a linked account");

			if (account is { EliteAccount.MinecraftAccounts.Count: > 1 })
				foreach (var mcAccount in account.EliteAccount.MinecraftAccounts) {
					var badgeToRemove = mcAccount.Badges.FirstOrDefault(x => x.BadgeId == badgeId);
					if (badgeToRemove is null) continue;

					context.UserBadges.Remove(badgeToRemove);
				}
		}

		// Add the badge to the user
		var userBadge = new UserBadge {
			BadgeId = badge.Id,
			MinecraftAccountId = playerUuid,
			Visible = true
		};

		player.Badges.Add(userBadge);
		context.UserBadges.Add(userBadge);

		await context.SaveChangesAsync();
		return new OkResult();
	}

	public async Task<ActionResult> RemoveBadgeFromUser(string playerUuid, int badgeId) {
		var uuid = await mojangService.GetUuid(playerUuid);

		var userBadge = await context.UserBadges
			.FirstOrDefaultAsync(a => a.MinecraftAccountId == uuid && a.BadgeId == badgeId);

		if (userBadge is null) return new NotFoundObjectResult("User badge not found");

		context.UserBadges.Remove(userBadge);
		await context.SaveChangesAsync();
		return new OkResult();
	}
}