using System.ComponentModel.DataAnnotations;
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