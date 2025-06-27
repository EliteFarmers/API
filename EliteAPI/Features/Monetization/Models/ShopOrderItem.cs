using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Features.Monetization.Models;

public class ShopOrderItem
{
    [Key]
    public ulong Id { get; set; }

    [ForeignKey(nameof(ShopOrder))]
    public Guid OrderId { get; set; }
    public ShopOrder ShopOrder { get; set; } = null!;

    [ForeignKey(nameof(Product))]
    public ulong ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}