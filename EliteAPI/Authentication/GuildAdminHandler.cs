using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

namespace EliteAPI.Authentication;

public class GuildAdminRequirement(GuildPermission permission) : IAuthorizationRequirement {
	public GuildPermission Permission { get; } = permission;
	public GuildAdminRequirement() : this(GuildPermission.Role) { }
}

public class GuildAdminHandler(
	IDiscordService discordService,
	IConnectionMultiplexer redis) 
	: AuthorizationHandler<GuildAdminRequirement>
{
	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GuildAdminRequirement requirement) {
		if (context.Resource is not HttpContext httpContext) return;
		if (httpContext.User.Identity?.IsAuthenticated is not true) return;
		
		// Get guild id from route
		var guildIdObject = httpContext.GetRouteValue("guildId");
		if (guildIdObject is null || !ulong.TryParse(guildIdObject.ToString(), out var guildId)) return;
		
		// Check cache for if user has permission
		var key = $"discord:guild_auth:{httpContext.User.GetId()}:{guildId}:{requirement.Permission}";
		var db = redis.GetDatabase();
		
		var valueKey = await db.StringGetAsync(key + ":value");
		if (valueKey.HasValue) {
			if (valueKey == "true") {
				context.Succeed(requirement);
			}
			return;
		}
		
		// Ensure only one instance of this job is running at a time
		if (await db.LockTakeAsync(key, "1", TimeSpan.FromMinutes(1))) {
			try {
				var member = await discordService.GetGuildMemberIfAdmin(httpContext.User, guildId, requirement.Permission);
				if (member is null) {
					await db.StringSetAsync(key + ":value", "false", TimeSpan.FromMinutes(5));
				} else {
					await db.StringSetAsync(key + ":value", "true", TimeSpan.FromMinutes(5));
					context.Succeed(requirement);
				}
			} finally {
				await db.LockReleaseAsync(key, "1");
			}
		}
	}
}