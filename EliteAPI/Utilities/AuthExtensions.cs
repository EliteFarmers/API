using System.Security.Claims;

namespace EliteAPI.Utilities;

public static class AuthExtensions {
	public static string? GetId(this ClaimsPrincipal user) {
		if (user.Identity?.IsAuthenticated is not true) {
			return null;
		}

		return user.FindFirstValue(ClaimTypes.NameIdentifier);
	}
	
	public static ulong? GetDiscordId(this ClaimsPrincipal user) {
		if (user.Identity?.IsAuthenticated is not true) {
			return null;
		}

		if (!ulong.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) {
			return null;
		}

		return id;
	}
}