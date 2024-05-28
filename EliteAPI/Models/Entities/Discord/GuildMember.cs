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
    
	[ForeignKey("Guild")]
	public ulong GuildId { get; set; }
	public Guild Guild { get; set; } = null!;
}