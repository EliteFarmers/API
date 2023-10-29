using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events; 

public class Event {
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    public bool Public { get; set; }
    public EventType Category { get; set; }
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

public enum EventType {
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
    
    [Column("StartConditions", TypeName = "jsonb")]
    public EventMemberStartConditions EventMemberStartConditions { get; set; } = new();

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

public class EventMemberStartConditions {
    public Dictionary<Crop, long> InitialCollection { get; set; } = new();
    public Dictionary<Crop, long> IncreasedCollection { get; set; } = new();
    public Dictionary<Crop, long> CountedCollection { get; set; } = new();
    public Dictionary<string, EventToolState> ToolStates { get; set; } = new();
    public Dictionary<string, long> Tools { get; set; } = new();
}

public class EventToolState {
    public required string SkyblockId { get; set; }
    public Crop? Crop { get; set; }
    
    public long FirstSeen { get; set; }
    public long LastSeen { get; set; }
    public bool IsActive { get; set; }

    public EventToolCounterState Counter { get; set; } = new();
    public EventToolCounterState Cultivating { get; set; } = new();
}

public class EventToolCounterState {
    public long Initial { get; set; }
    public long Previous { get; set; }
    public long Current { get; set; }
    public long Uncounted { get; set; }

    public EventToolCounterState() { }
    
    public EventToolCounterState(long initial) {
        Initial = initial;
        Previous = initial;
        Current = initial;
    }
    
    public long IncreaseFromInitial() => Current - Initial - Uncounted;
    public long IncreaseFromPrevious() => Current - Previous - Uncounted;
}