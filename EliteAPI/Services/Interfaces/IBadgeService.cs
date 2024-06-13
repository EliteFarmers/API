using EliteAPI.Models.Entities.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IBadgeService {
    public Task<Badge?> GetBadgeById(int id);
    public Task<ActionResult> AddBadgeToUser(string playerUuid, int badgeId);
    public Task<ActionResult> RemoveBadgeFromUser(string playerUuid, int badgeId);
}