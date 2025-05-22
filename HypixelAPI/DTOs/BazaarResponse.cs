using System.Text.Json.Serialization;

namespace HypixelAPI.DTOs;

public class BazaarResponse {
	public bool Success { get; set; }
	public long LastUpdated { get; set; }
	public Dictionary<string, BazaarItem> Products { get; set; } = new();
}

public class BazaarItem {
	public string? ProductId { get; set; }
	/// <summary>
	/// The current top 30 buy orders
	/// </summary>
	[JsonPropertyName("buy_summary")]
	public List<BazaarOrder> BuySummary { get; set; } = [];
	/// <summary>
	/// The current top 30 sell orders
	/// </summary>
	[JsonPropertyName("sell_summary")]
	public List<BazaarOrder> SellSummary { get; set; } = [];
	
	[JsonPropertyName("quick_status")]
	public ProductQuickStatus QuickStatus { get; set; } = new();
}

public class BazaarOrder {
	public int Amount { get; set; }
	public double PricePerUnit { get; set; }
	public int Orders { get; set; }
}

public class ProductQuickStatus {
	public string? ProductId { get; set; }
	public double SellPrice { get; set; }
	public long SellVolume { get; set; }
	public long SellMovingWeekly { get; set; }
	public int SellOrders { get; set; }
	public double BuyPrice { get; set; }
	public long BuyVolume { get; set; }
	public long BuyMovingWeekly { get; set; }
	public int BuyOrders { get; set; }
}