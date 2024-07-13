using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliteAPI.Models.Entities.Monetization;

// https://discord.com/developers/docs/monetization/entitlements#entitlement-object-entitlement-types
public enum EntitlementType {
	Purchase = 1,
	PremiumSubscription = 2,
	DeveloperGift = 3,
	TestModePurchase = 4,
	FreePurchase = 5,
	UserGift = 6,
	PremiumPurchase = 7,
	ApplicationSubscription = 8
}

public enum EntitlementTarget {
	None = 0,
	User = 1,
	Guild = 2
}

public class Entitlement {
	[Key]
	public ulong Id { get; set; }
	public EntitlementType Type { get; set; }
	public EntitlementTarget Target { get; set; } = EntitlementTarget.None;

	[ForeignKey("Product")]
	[JsonPropertyName("sku_id")]
	public ulong ProductId { get; set; }
	public Product Product { get; set; } = null!;
	
	public bool Deleted { get; set; }
	public bool? Consumed { get; set; }
	
	public DateTimeOffset? StartDate { get; set; }
	public DateTimeOffset? EndDate { get; set; }
}