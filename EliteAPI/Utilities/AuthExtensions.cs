using System.Security.Claims;
using EliteAPI.Features.Auth.Models;

namespace EliteAPI.Utilities;

public static class AuthExtensions
{
	extension(ClaimsPrincipal user)
	{
		public string? GetId() {
			if (user.Identity?.IsAuthenticated is not true) return null;

			return user.FindFirstValue(ClaimNames.NameId);
		}

		public ulong? GetDiscordId() {
			if (user.Identity?.IsAuthenticated is not true) return null;

			if (!ulong.TryParse(user.FindFirstValue(ClaimNames.NameId), out var id)) return null;

			return id;
		}

		public string? GetDiscordUsername() {
			if (user.Identity?.IsAuthenticated is not true) return null;
			return user.FindFirstValue(ClaimNames.Name);
		}
		
		public bool IsSupportOrHigher() {
			if (user.Identity?.IsAuthenticated is not true) return false;

			return user.IsInRole(ApiUserPolicies.Support) || user.IsInRole(ApiUserPolicies.Moderator) || user.IsInRole(ApiUserPolicies.Admin);
		}
		
		public bool IsModeratorOrHigher() {
			if (user.Identity?.IsAuthenticated is not true) return false;

			return user.IsInRole(ApiUserPolicies.Moderator) || user.IsInRole(ApiUserPolicies.Admin);
		}
		
		public bool IsAdmin() {
			if (user.Identity?.IsAuthenticated is not true) return false;

			return user.IsInRole(ApiUserPolicies.Admin);
		}
	}
}