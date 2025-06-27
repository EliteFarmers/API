using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;

namespace EliteAPI.Features.Monetization.Models;

public class ShopOrder
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey(nameof(Buyer))]
    public ulong BuyerId { get; set; }
    public EliteAccount? Buyer { get; set; } = null!;
    
    [ForeignKey(nameof(Recipient))]
    public ulong? RecipientId { get; set; }
    public EliteAccount? Recipient { get; set; }

    [ForeignKey("Guild")]
    public ulong? RecipientGuildId { get; set; }
    public Guild? RecipientGuild { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public PaymentProvider Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "USD";
    
    public virtual ICollection<ShopOrderItem> OrderItems { get; set; } = new List<ShopOrderItem>();
}

public enum PaymentProvider
{
    None = 0,
    Discord = 1,
    Stripe = 2,
    ManualGift = 3,
}

public enum OrderStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3,
    Disputed = 4,
}