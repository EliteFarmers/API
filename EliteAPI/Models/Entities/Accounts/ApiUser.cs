﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Models.Entities.Accounts;

public class ApiUser : IdentityUser {
	[MaxLength(256)]
	public string? DiscordAccessToken { get; set; }
	public DateTimeOffset DiscordAccessTokenExpires { get; set; }
	
	[MaxLength(256)]
	public string? DiscordRefreshToken { get; set; }
	public DateTimeOffset DiscordRefreshTokenExpires { get; set; }
	
	[ForeignKey("Account")]
	public ulong? AccountId { get; set; }
	public EliteAccount Account { get; set; } = null!;
}

public static class ApiUserClaims {
	public const string Avatar = "Avatar";
	public const string Ign = "Ign";
}

public static class ApiUserRoles {
	public const string Admin = "Admin";
	public const string Moderator = "Moderator";
	public const string Support = "Support";
	public const string Wiki = "Wiki";
	public const string User = "User";
}