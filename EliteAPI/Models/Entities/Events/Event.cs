using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events; 

public class Event {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    [MaxLength(64)]
    public required string Name { get; set; }
    
    [MaxLength(1024)]
    public string? Description { get; set; }
    [MaxLength(1024)]
    public string? Rules { get; set; }
    [MaxLength(1024)]
    public string? PrizeInfo { get; set; }
    
    public string? Image { get; set; }
    
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public bool DynamicStartTime { get; set; }
    
    public List<EventMember> Members { get; set; } = new();
    public List<BlockedUser> BlockedUsers { get; set; } = new();
    
    [ForeignKey("Owner")]
    public ulong OwnerId { get; set; }
    public AccountEntity Owner { get; set; } = null!;
    
    [ForeignKey("Guild")]
    public ulong GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
}

public class EventMember {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public bool Active { get; set; }
    
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public bool Disqualified { get; set; }
    [MaxLength(128)]
    public string? Reason { get; set; }
    
    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember ProfileMember { get; set; } = null!;
    
    [ForeignKey("Event")]
    public ulong EventId { get; set; }
    public Event Event { get; set; } = null!;
    
    [ForeignKey("User")]
    public ulong UserId { get; set; }
    public AccountEntity User { get; set; } = null!;
}