using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.DTOs.Outgoing;

public class EventDetailsDto {
    /// <summary>
    /// Event id as a string
    /// </summary>
    public required string Id { get; set; }
    /// <summary>
    /// Name of the event
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Type of the event
    /// </summary>
    public EventType Type { get; set; }
    /// <summary>
    /// Team mode of the event
    /// </summary>
    public string? Mode { get; set; } = EventTeamMode.Solo;
    
    /// <summary>
    /// Event description
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Event rules
    /// </summary>
    public string? Rules { get; set; }
    /// <summary>
    /// Event prize information
    /// </summary>
    public string? PrizeInfo { get; set; }
    
    /// <summary>
    /// Image URL for the event banner
    /// </summary>
    public string? Banner { get; set; }
    /// <summary>
    /// Image URL for the event thumbnail
    /// </summary>
    public string? Thumbnail { get; set; }
    
    /// <summary>
    /// Start time of the event as a string in Unix seconds
    /// </summary>
    public string? StartTime { get; set; }
    /// <summary>
    /// Join time of the event as a string in Unix seconds
    /// </summary>
    public string? JoinUntilTime { get; set; }
    /// <summary>
    /// End time of the event as a string in Unix seconds
    /// </summary>
    public string? EndTime { get; set; }
    
    /// <summary>
    /// Currently unused
    /// </summary>
    public bool DynamicStartTime { get; set; }
    /// <summary>
    /// Event status
    /// </summary>
    public bool Active { get; set; }
    
    /// <summary>
    /// Max amount of teams allowed in the event, 0 if solo event, -1 if unlimited
    /// </summary>
    public int MaxTeams { get; set; }
    
    /// <summary>
    /// Max amount of members allowed in a team, 0 if solo event, -1 if unlimited
    /// </summary>
    public int MaxTeamMembers { get; set; }
    
    /// <summary>
    /// Discord role id required to participate in the event
    /// </summary>
    public string? RequiredRole { get; set; }
    /// <summary>
    /// Discord role id blocked from participating in the event
    /// </summary>
    public string? BlockedRole { get; set; }
    /// <summary>
    /// Discord server id as a string
    /// </summary>
    public string? GuildId { get; set; }
    /// <summary>
    /// Data specific to the event
    /// </summary>
    public object? Data { get; set; }
}

public class EventTeamDto {
    public int Id { get; set; }
    public string? EventId { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Score { get; set; }
    public string? OwnerId { get; set; }
}

public class EventTeamWithMembersDto : EventTeamDto {
    public List<EventMemberDto> Members { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JoinCode { get; set; }
}


public class CreateEventTeamDto {
    [MaxLength(32)]
    public string? Name { get; set; }
    [MaxLength(7)]
    public string? Color { get; set; }
}

public class UpdateEventTeamDto {
    [MaxLength(32)]
    public string? Name { get; set; }
    [MaxLength(7)]
    public string? Color { get; set; }
}

public class EventMemberDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    public string? ProfileId { get; set; }
    public required string EventId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TeamId { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public string? Score { get; set; }
    public object? Data { get; set; }
    public string? LastUpdated { get; set; }
    
    public bool Disqualified { get; set; }
    
    [MaxLength(128)] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Notes { get; set; }
}

public class EventMemberDetailsDto {
    public string? PlayerUuid { get; set; }
    public string? ProfileId { get; set; }
    public string? PlayerName { get; set; }
    public required string EventId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TeamId { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public string? Score { get; set; }
    public string? LastUpdated { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; } = null;
}

public class EventMemberBannedDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TeamId { get; set; }
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
    public long? JoinTime { get; set; }
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
    /// Unix timestamp for the latest time a new member can join the event in seconds
    /// </summary>
    public long? JoinTime { get; set; }
    
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

public class CreateEventMemberDto  {
    public ulong EventId { get; set; }
    public EventType Type { get; set; }
    
    public Guid ProfileMemberId { get; set; }
    public double Score { get; set; }
    
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public ulong UserId { get; set; }
    
    public ProfileMember ProfileMember { get; set; }
}