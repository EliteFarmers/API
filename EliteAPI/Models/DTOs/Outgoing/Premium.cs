namespace EliteAPI.Models.DTOs.Outgoing;

public class PremiumDto
{
    public List<PurchaseDto> Purchases { get; set; } = new();
    public bool Active { get; set; } = false;
}

public class PurchaseDto
{
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