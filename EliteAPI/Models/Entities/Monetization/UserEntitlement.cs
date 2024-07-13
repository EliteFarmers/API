using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Models.Entities.Monetization;

public class UserEntitlement : Entitlement {
	[ForeignKey("Account")]
	[MaxLength(22)]
	public ulong AccountId { get; set; }
	public EliteAccount Account { get; set; } = null!;

	public UserEntitlement() {
		Target = EntitlementTarget.User;
	}
}