using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Discord;

public class GuildChannel {
	[Key] public ulong Id { get; set; }
	[MaxLength(128)] public required string Name { get; set; }

	public int Position { get; set; }
	public int Type { get; set; }
	public ulong BotPermissions { get; set; }

	[ForeignKey("Guild")] public ulong GuildId { get; set; }
	public Guild Guild { get; set; } = null!;

	public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}