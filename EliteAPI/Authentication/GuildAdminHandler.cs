using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Authentication;

public class GuildAdminRequirement(GuildPermission permission) : IAuthorizationRequirement {
	public GuildPermission Permission { get; } = permission;
	public GuildAdminRequirement() : this(GuildPermission.Role) { }
}

public class GuildAdminHandler(
	IDiscordService discordService,
	UserManager<ApiUser> userManager) 
	: AuthorizationHandler<GuildAdminRequirement>
{
	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GuildAdminRequirement requirement) {
		if (context.Resource is not HttpContext httpContext) return;
		// Get guild id from route
		var guildIdObject = httpContext.GetRouteValue("guildId");
		if (guildIdObject is null || !ulong.TryParse(guildIdObject.ToString(), out var guildId)) return;
		
		var user = await userManager.GetUserAsync(httpContext.User);
		if (user is null) return;

		var member = await discordService.GetGuildMemberIfAdmin(user, guildId, requirement.Permission);
		if (member is null) return;

		context.Succeed(requirement);
	}
}