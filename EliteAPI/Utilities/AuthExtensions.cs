using System.Security.Claims;

namespace EliteAPI.Utilities;

public static class AuthExtensions {
	public static string? GetId(this ClaimsPrincipal user) {
		if (user.Identity?.IsAuthenticated != true) {
			return null;
		}

		return user.FindFirstValue(ClaimTypes.NameIdentifier);
	}
}