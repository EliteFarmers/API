using System.Security.Claims;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Utilities;

public static class AuthExtensions {
	public static string? GetId(this ClaimsPrincipal user) {
		if (user.Identity?.IsAuthenticated is not true) {
			return null;
		}

		return user.FindFirstValue(ClaimNames.NameId);
	}
	
	public static ulong? GetDiscordId(this ClaimsPrincipal user) {
		if (user.Identity?.IsAuthenticated is not true) {
			return null;
		}

		if (!ulong.TryParse(user.FindFirstValue(ClaimNames.NameId), out var id)) {
			return null;
		}

		return id;
	}
}