using EliteAPI.Features.Account.Services;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

namespace EliteAPI.Authentication;

public class GuildAdminRequirement(GuildPermission permission) : IAuthorizationRequirement
{
	public GuildPermission Permission { get; } = permission;

	public GuildAdminRequirement() : this(GuildPermission.Role) { }
}

public class GuildAdminHandler(
	IDiscordService discordService,
	IConnectionMultiplexer redis)
	: AuthorizationHandler<GuildAdminRequirement>
{
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);
	private const int MaxRetries = 5;
	private const int RetryDelayMs = 100;

	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
		GuildAdminRequirement requirement) {
		if (context.Resource is not HttpContext httpContext ||
		    httpContext.User.Identity?.IsAuthenticated is not true) return;

		var guildIdObject = httpContext.GetRouteValue("guildId") ?? httpContext.GetRouteValue("DiscordId");
		if (guildIdObject is null || !ulong.TryParse(guildIdObject.ToString(), out var guildId)) {
			context.Fail(new AuthorizationFailureReason(this, "Guild ID not found in route."));
			return;
		}

		var userId = httpContext.User.GetId();
		if (userId is null) {
			context.Fail(new AuthorizationFailureReason(this, "User ID not found in token."));
			return;
		}

		var cacheKey = $"discord:guild_auth:{userId}:{guildId}:{requirement.Permission}";
		var lockKey = $"{cacheKey}:lock";
		var db = redis.GetDatabase();

		// Poll the cache until a value is found or retries are exhausted
		for (var i = 0; i < MaxRetries; i++) {
			var cachedValue = await db.StringGetAsync(cacheKey);
			if (cachedValue.HasValue) {
				if (cachedValue == "true") context.Succeed(requirement);

				return; // Cached value found, but no permission granted
			}

			// Get lock to prevent multiple requests from checking the same guild at the same time
			if (await db.LockTakeAsync(lockKey, "1", LockDuration))
				try {
					// Re-check cache immediately after acquiring lock to handle race condition
					cachedValue = await db.StringGetAsync(cacheKey);
					if (cachedValue.HasValue) {
						if (cachedValue == "true") context.Succeed(requirement);

						return; // Cached value found, but no permission granted
					}

					// Check if the user is an admin in the guild
					var member =
						await discordService.GetGuildMemberIfAdmin(httpContext.User, guildId, requirement.Permission);
					var isAuthorized = member is not null;

					await db.StringSetAsync(cacheKey, isAuthorized.ToString().ToLower(), CacheDuration);

					if (isAuthorized) context.Succeed(requirement);

					return; // No permission granted
				}
				finally {
					await db.LockReleaseAsync(lockKey, "1");
				}

			// If we couldn't acquire the lock, wait and retry
			// This is from other requests holding the lock
			await Task.Delay(RetryDelayMs);
		}

		// If we exit the loop, it means we timed out waiting for the lock/cache.
		// Authorization fails by default. You may want to log this scenario.
		context.Fail(new AuthorizationFailureReason(this, "Timeout while waiting for authorization check."));
	}
}