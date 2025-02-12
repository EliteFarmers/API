using Microsoft.AspNetCore.Authorization;

namespace EliteAPI.Authentication;

public enum GuildPermission {
	Admin,
	Manager,
	Role,
}

public class GuildAdminAuthorizeAttribute : AuthorizeAttribute 
{
	public static readonly string PolicyPrefix = "GuildAdmin";
	public GuildPermission Permission { get; }
	
	public GuildAdminAuthorizeAttribute(GuildPermission permission = GuildPermission.Role) {
		Permission = permission;
		Policy = $"{PolicyPrefix}{Enum.GetName(permission)}";
	}
}

public static class GuildAdminPolicies 
{
	public static AuthorizationBuilder AddGuildAdminPolicies(this AuthorizationBuilder builder) {
		foreach (var permission in Enum.GetValues<GuildPermission>()) {
			builder.AddPolicy($"{GuildAdminAuthorizeAttribute.PolicyPrefix}{Enum.GetName(permission)}", policy => {
				policy.RequireAuthenticatedUser();
				policy.Requirements.Add(new GuildAdminRequirement(permission));
			});
		}

		return builder;
	}
}