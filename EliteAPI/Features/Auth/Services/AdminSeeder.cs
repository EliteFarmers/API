using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Auth.Services;

public interface IAdminSeeder
{
    Task SeedAdminUserAsync();
}

public class AdminSeeder(
	UserManager<ApiUser> userManager,
	IOptions<SeedSettings> seedOptions,
	ILogger<AdminSeeder> logger) : IAdminSeeder
{
	private readonly SeedSettings _seedSettings = seedOptions.Value;

    public async Task SeedAdminUserAsync()
    {
        var adminId = _seedSettings.AdminUserId;
        if (string.IsNullOrEmpty(adminId))
        {
            return;
        }

        // Check if there are already any admin users
        var existingAdmins = await userManager.GetUsersInRoleAsync(ApiUserPolicies.Admin);
        if (existingAdmins.Any())
        {
            logger.LogInformation("Admin users already exist, skipping seeding.");
            return;
        }

        // Find the user by ID
        var user = await userManager.FindByIdAsync(adminId);
        if (user == null)
        {
            logger.LogWarning("Configured seed admin user ID {AdminId} not found in database. User must log in first.", adminId);
            return;
        }

        // Assign Admin role
        var result = await userManager.AddToRoleAsync(user, ApiUserPolicies.Admin);
        if (result.Succeeded)
        {
            logger.LogInformation("Successfully granted Admin role to user {AdminId}.", adminId);
        }
        else
        {
            logger.LogError("Failed to grant Admin role to user {AdminId}: {Errors}", adminId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
