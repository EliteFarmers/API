using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Features.Monetization.Models;

public class ProductAccess
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey(nameof(Product))]
    public ulong ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    [ForeignKey(nameof(User))]
    public ulong? UserId { get; set; }
    public EliteAccount? User { get; set; }

    [ForeignKey(nameof(Guild))]
    public ulong? GuildId { get; set; }
    public Guild? Guild { get; set; }

    [ForeignKey(nameof(SourceOrder))]
    public Guid? SourceOrderId { get; set; }
    public ShopOrder? SourceOrder { get; set; }
    
    public DateTimeOffset StartDate { get; set; }
    /// <summary>
    /// Null for lifetime access.
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
    
    public bool IsActive => !Revoked && StartDate <= DateTimeOffset.UtcNow && (EndDate == null || EndDate >= DateTimeOffset.UtcNow);
    public bool Revoked { get; set; } = false;
    public bool Consumed { get; set; } = false;
    
    public bool HasWeightStyle(int weightStyleId) {
        return Product?.ProductWeightStyles is {} list && list.Exists(l => l.WeightStyleId == weightStyleId);
    }
}