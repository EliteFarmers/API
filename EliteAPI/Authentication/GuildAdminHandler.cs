using EliteAPI.Background.Discord;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Quartz;

namespace EliteAPI.Authentication;

public class GuildAdminRequirement(GuildPermission permission) : IAuthorizationRequirement {
	public GuildPermission Permission { get; } = permission;
	public GuildAdminRequirement() : this(GuildPermission.Role) { }
}

public class GuildAdminHandler(
	IDiscordService discordService,
	UserManager<ApiUser> userManager,
	ISchedulerFactory schedulerFactory) 
	: AuthorizationHandler<GuildAdminRequirement>
{
	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GuildAdminRequirement requirement) {
		if (context.Resource is not HttpContext httpContext) return;
		// Get guild id from route
		var guildIdObject = httpContext.GetRouteValue("guildId");
		if (guildIdObject is null || !ulong.TryParse(guildIdObject.ToString(), out var guildId)) return;
		
		var user = await userManager.GetUserAsync(httpContext.User);
		if (user is null) return;
		
		// Refresh Discord access token if it's expired
		if (user.DiscordRefreshToken is not null
			&& user.DiscordAccessTokenExpires > DateTimeOffset.UtcNow 
		    && user.DiscordRefreshTokenExpires < DateTimeOffset.UtcNow) 
		{
			var data = new JobDataMap { { "AccountId", user.Id } };
			var scheduler = await schedulerFactory.GetScheduler();
			await scheduler.TriggerJob(RefreshAuthTokenBackgroundTask.Key, data);
		}

		var member = await discordService.GetGuildMemberIfAdmin(user, guildId, requirement.Permission);
		if (member is null) return;

		context.Succeed(requirement);
	}
}