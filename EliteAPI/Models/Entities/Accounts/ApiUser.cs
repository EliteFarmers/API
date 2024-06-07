using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using EliteAPI.Models.Entities.Discord;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Models.Entities.Accounts;

public class ApiUser : IdentityUser {
	[MaxLength(256)]
	public string? DiscordAccessToken { get; set; }
	public DateTimeOffset DiscordAccessTokenExpires { get; set; }
	
	[MaxLength(256)]
	public string? DiscordRefreshToken { get; set; }
	public DateTimeOffset DiscordRefreshTokenExpires { get; set; }
	
	public List<GuildMember> GuildMemberships { get; set; } = [];
	public DateTimeOffset GuildsLastUpdated { get; set; } = DateTimeOffset.UtcNow;
	
	[ForeignKey("Account")]
	public ulong? AccountId { get; set; }
	public EliteAccount Account { get; set; } = null!;
}

public static class ApiUserClaims {
	public const string Avatar = "Avatar";
	public const string Ign = "Ign";
	public const string DiscordAccessExpires = "Dexp";
}

public static class ApiUserPolicies {
	public const string Admin = "Admin";
	public const string Moderator = "Moderator";
	public const string Support = "Support";
	public const string Wiki = "Wiki";
	public const string User = "User";
}

public static class ApiUserExtensions {
	public static bool AccessTokenExpired(this ClaimsPrincipal user) {
		var value = user.FindFirstValue(ApiUserClaims.DiscordAccessExpires);
		if (value is null || !long.TryParse(value, out var seconds)) return true;
		
		return seconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}
}