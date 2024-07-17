using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Discord;

namespace EliteAPI.Models.Entities.Monetization;

public class GuildEntitlement : Entitlement {
	[ForeignKey("User")]
	public ulong GuildId { get; set; }
	public Guild Guild { get; set; } = null!;

	public GuildEntitlement() {
		Target = EntitlementTarget.Guild;
	}
}