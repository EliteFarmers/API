using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;

namespace EliteAPI.Features.Monetization.Models;

public class UserEntitlement : Entitlement {
	[ForeignKey("Account")]
	[MaxLength(22)]
	public ulong AccountId { get; set; }

	public EliteAccount Account { get; set; } = null!;

	public bool HasWeightStyle(int weightStyleId) {
		return Product?.ProductWeightStyles is { } list && list.Exists(l => l.WeightStyleId == weightStyleId);
	}

	public UserEntitlement() {
		Target = EntitlementTarget.User;
	}
}