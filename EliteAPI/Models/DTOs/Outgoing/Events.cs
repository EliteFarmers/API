using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class EventDetailsDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public EventType Type { get; set; }
    
    public string? Description { get; set; }
    public string? Rules { get; set; }
    public string? PrizeInfo { get; set; }
    
    public string? Banner { get; set; }
    public string? Thumbnail { get; set; }
    
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    
    public bool DynamicStartTime { get; set; }
    public bool Active { get; set; }
    
    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }
    public string? GuildId { get; set; }
    public object? Data { get; set; }
}

public class EventMemberDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    public string? ProfileId { get; set; }
    public required string EventId { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public string? Score { get; set; }

    public object? Data { get; set; }
    public string? LastUpdated { get; set; }
    
    public bool Disqualified { get; set; }
    [MaxLength(128)]
    public string? Notes { get; set; }
}

public class EventMemberDetailsDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    public string? ProfileId { get; set; }
    public required string EventId { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public string? Score { get; set; }
    public string? LastUpdated { get; set; }
}

public class EventMemberBannedDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    
    public string? Score { get; set; }
    public string? Notes { get; set; }
    
    public string? LastUpdated { get; set; }
}

public class EditEventDto {
    public string? Name { get; set; }
    public string? Type { get; set; }
    
    public string? Description { get; set; }
    public string? Rules { get; set; }
    public string? PrizeInfo { get; set; }
    
    public string? Banner { get; set; }
    public string? Thumbnail { get; set; }
    
    public long? StartTime { get; set; }
    public long? EndTime { get; set; }
    
    public bool? DynamicStartTime { get; set; }
    public bool? Active { get; set; }
    
    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }
    
    public string? GuildId { get; set; }
}

public class EditWeightEventDto : EditEventDto {
    public WeightEventData? Data { get; set; }
}

public class EditMedalEventDto : EditEventDto {
    public MedalEventData? Data { get; set; }
}

public class CreateEventDto {
    /// <summary>
    /// The name of the event
    /// </summary>
    [Required]
    [MaxLength(64)]
    public required string Name { get; set; }
    
    /// <summary>
    /// The type of the event
    /// </summary>
    public EventType? Type { get; set; }
    
    /// <summary>
    /// The Discord server id as a string for the event
    /// </summary>
    [Required]
    public required string GuildId { get; set; }
    
    /// <summary>
    /// An optional description for the event
    /// </summary>
    [MaxLength(1024)]
    public string? Description { get; set; }
    /// <summary>
    /// An optional set of rules for the event
    /// </summary>
    [MaxLength(1024)]
    public string? Rules { get; set; }
    
    /// <summary>
    /// An optional description of prizes for the event
    /// </summary>
    [MaxLength(1024)]
    public string? PrizeInfo { get; set; }
    
    /// <summary>
    /// An image URL for the event banner
    /// </summary>
    [MaxLength(256)]
    public string? Banner { get; set; }
    
    /// <summary>
    /// An image URL for the event thumbnail
    /// </summary>
    [MaxLength(256)]
    public string? Thumbnail { get; set; }
    
    /// <summary>
    /// Unix timestamp for the start time of the event in seconds
    /// </summary>
    [Required]
    public long? StartTime { get; set; }
    
    /// <summary>
    /// Unix timestamp for the end time of the event in seconds
    /// </summary>
    [Required]
    public long? EndTime { get; set; }
    
    /// <summary>
    /// Currently unused
    /// </summary>
    public bool? DynamicStartTime { get; set; }
    
    /// <summary>
    /// A Discord role id that is required to participate in the event
    /// </summary>
    [MaxLength(24)]
    public string? RequiredRole { get; set; }
    
    /// <summary>
    /// A Discord role id that is blocked from participating in the event
    /// </summary>
    [MaxLength(24)]
    public string? BlockedRole { get; set; }
}

public class CreateWeightEventDto : CreateEventDto {
    /// <summary>
    /// Data specific to the weight event
    /// </summary>
    public WeightEventData? Data { get; set; }

    public CreateWeightEventDto() {
        Type = EventType.FarmingWeight;
    }
}

public class CreateMedalEventDto : CreateEventDto {
    /// <summary>
    /// Data specific to the medal event
    /// </summary>
    public MedalEventData? Data { get; set; }

    public CreateMedalEventDto() {
        Type = EventType.Medals;
    }
}