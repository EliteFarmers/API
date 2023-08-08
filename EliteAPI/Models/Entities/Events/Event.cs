using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events; 

public class Event {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    public bool Public { get; set; }
    public EventCategory Category { get; set; }
    public string? Target { get; set; }
    
    [MaxLength(64)]
    public required string Name { get; set; }
    
    [MaxLength(1024)]
    public string? Description { get; set; }
    [MaxLength(1024)]
    public string? Rules { get; set; }
    [MaxLength(1024)]
    public string? PrizeInfo { get; set; }
    
    public string? Banner { get; set; }
    public string? Thumbnail { get; set; }
    
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public bool DynamicStartTime { get; set; }
    public bool Active { get; set; }
    public List<EventMember> Members { get; set; } = new();
    
    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }
    
    [ForeignKey("Owner")]
    public ulong OwnerId { get; set; }
    public EliteAccount Owner { get; set; } = null!;
    
    [ForeignKey("Guild")]
    public ulong GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
}

public enum EventCategory {
    FarmingWeight,
    Collection,
    Experience
}

public enum EventMemberStatus {
    Inactive,
    Active,
    Left,
    Disqualified
}

public class EventMember {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public double AmountGained { get; set; }
    
    [Column(TypeName = "jsonb")]
    public StartConditions StartConditions { get; set; } = new();

    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    [MaxLength(128)]
    public string? Notes { get; set; }
    
    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember ProfileMember { get; set; } = null!;
    
    [ForeignKey("Event")]
    public ulong EventId { get; set; }
    public Event Event { get; set; } = null!;
    
    [ForeignKey("User")]
    public ulong UserId { get; set; }
    public EliteAccount User { get; set; } = null!;
}

public class StartConditions {
    public Dictionary<Crop, long> Collection { get; set; } = new();
    public Dictionary<string, long> Tools { get; set; } = new();
}