using EliteAPI.Features.Account.Models;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Account.Services;

public interface IBadgeService {
    public Task<Badge?> GetBadgeById(int id);
    public Task<ActionResult> AddBadgeToUser(string playerUuid, int badgeId);
    public Task<ActionResult> RemoveBadgeFromUser(string playerUuid, int badgeId);
}