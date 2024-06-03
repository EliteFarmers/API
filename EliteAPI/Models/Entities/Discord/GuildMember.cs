using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Models.Entities.Discord;

public class GuildMember {
	[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
    
	[ForeignKey("Account")] [MaxLength(32)]
	public string? AccountId { get; set; }
	public ApiUser? Account { get; set; }
    
	public ulong Permissions { get; set; }
	public List<GuildMemberRole> Roles { get; set; } = [];
	
	[ForeignKey("Guild")]
	public ulong GuildId { get; set; }
	public Guild Guild { get; set; } = null!;
	
	public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

public class GuildMemberRole {
	[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	public ulong RoleId { get; set; }
	
	[ForeignKey("Role")]
	public int MemberId { get; set; }
	public GuildMember Member { get; set; } = null!;
}