using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Accounts;

public class Badge {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [MaxLength(50)]
    public required string Name { get; set; }
    [MaxLength(1024)]
    public required string Description { get; set; }
    [MaxLength(512)]
    public required string Requirements { get; set; }
    [MaxLength(256)]
    public required string ImageId { get; set; }
}

public class UserBadge {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public bool Visible { get; set; }
    public int Order { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    [ForeignKey("Badge")]
    public int BadgeId { get; set; }
    public Badge Badge { get; set; } = null!;
    
    [ForeignKey("User")] [MaxLength(36)]
    public required string MinecraftAccountId { get; set; }
    public MinecraftAccount MinecraftAccount { get; set; } = null!;
}