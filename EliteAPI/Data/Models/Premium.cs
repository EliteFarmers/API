using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models;

public class Premium
{
    public int Id { get; set; }
    public List<Purchase> Purchases { get; set; } = new();
    public bool Active { get; set; } = false;

    [ForeignKey("Account")] public int AccountId { get; set; }
    public required Account Account { get; set; }
}

public class Purchase
{
    public int Id { get; set; }
    public DateTime PurchasedTime { get; set; }
    public PurchaseType PurchaseType { get; set; }
    public decimal Price { get; set; } = 0;
}

public enum PurchaseType
{
    Donation = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
}