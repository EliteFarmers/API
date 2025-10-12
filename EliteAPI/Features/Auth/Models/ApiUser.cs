using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IdentityModel.Tokens.Jwt;
using EliteAPI.Features.Account.Models;
using EliteAPI.Models.Entities.Discord;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Features.Auth.Models;

public class ApiUser : IdentityUser {
	[MaxLength(256)] public string? DiscordAccessToken { get; set; }
	public DateTimeOffset DiscordAccessTokenExpires { get; set; }

	[MaxLength(256)] public string? DiscordRefreshToken { get; set; }
	public DateTimeOffset DiscordRefreshTokenExpires { get; set; }

	public List<GuildMember> GuildMemberships { get; set; } = [];
	public DateTimeOffset GuildsLastUpdated { get; set; } = DateTimeOffset.UtcNow;

	[ForeignKey("Account")] public ulong? AccountId { get; set; }
	public EliteAccount Account { get; set; } = null!;
}

public static class ClaimNames {
	public const string Role = "role";
	public const string Name = JwtRegisteredClaimNames.Name;
	public const string NameId = JwtRegisteredClaimNames.NameId;
	public const string Jti = JwtRegisteredClaimNames.Jti;

	public const string Avatar = "Avatar";
	public const string Ign = "Ign";
	public const string FormattedIgn = "FIgn";
	public const string Uuid = "Uuid";
}

public static class ApiUserPolicies {
	public const string Admin = "Admin";
	public const string Moderator = "Moderator";
	public const string Support = "Support";
	public const string Wiki = "Wiki";
	public const string User = "User";
}