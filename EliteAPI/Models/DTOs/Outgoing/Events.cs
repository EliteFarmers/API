using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Images;
using Microsoft.AspNetCore.Mvc;

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
    public ImageAttachmentDto? Banner { get; set; }
    
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
    /// Event approval status
    /// </summary>
    public bool Approved { get; set; }
    
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
    public string? OwnerUuid  { get; set; }
}

public class EventTeamWithMembersDto : EventTeamDto {
    public List<EventMemberDto> Members { get; set; } = [];
    
    /// <summary>
    /// Join code for the team, only populated if authenticated user is the owner
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JoinCode { get; set; }
}

public class EventDefaultsDto {
    public Dictionary<Crop, double> CropWeights { get; set; } = new();
    public Dictionary<ContestMedal, int> MedalValues { get; set; } = new();
    public Dictionary<Pest, int> PestWeights { get; set; } = new();
}

public class CreateEventTeamDto {
    /// <summary>
    /// An array of strings for the team name, example: [ "Bountiful", "Farmers" ]
    /// </summary>
    [MinLength(1), MaxLength(3)]
    public List<string>? Name { get; set; }
    [MaxLength(7)]
    public string? Color { get; set; }
}

public class UpdateEventTeamDto {
    /// <summary>
    /// An array of strings for the team name, example: [ "Bountiful", "Farmers" ]
    /// </summary>
    [MinLength(1), MaxLength(3)]
    public List<string>? Name { get; set; }
    
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

public class ProfileEventMemberDto {
    public required string EventId { get; set; }
    public required string EventName { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TeamId { get; set; }
    public EventMemberStatus Status { get; set; }

    /// <summary>
    /// Currently not populated
    /// </summary>
    public int Rank { get; set; }
    public string? Score { get; set; }
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
    public bool? Disqualified { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; } = null;
}

public class AdminEventMemberDto : EventMemberDetailsDto {
    public int Id { get; set; }
}

public class EditEventDto {
    public string? Name { get; set; }
    public string? Type { get; set; }
    
    public string? Description { get; set; }
    public string? Rules { get; set; }
    public string? PrizeInfo { get; set; }
    
    public long? StartTime { get; set; }
    public long? JoinTime { get; set; }
    public long? EndTime { get; set; }
    
    public bool? DynamicStartTime { get; set; }
    public bool? Active { get; set; }
    
    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }
    
    public string? GuildId { get; set; }
    
    public WeightEventData? WeightData { get; set; }
    public MedalEventData? MedalData { get; set; }
    public PestEventData? PestData { get; set; }
    public CollectionEventData? CollectionData { get; set; }
}

public class EditEventBannerDto {
    [FromForm(Name = "Image"), AllowedFileExtensions]
    public IFormFile? Image { get; set; }
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

    /// <summary>
    /// Max amount of teams allowed in the event, 0 if solo event, -1 if unlimited
    /// </summary>
    public int MaxTeams { get; set; } = 0;

    /// <summary>
    /// Max amount of members allowed in a team, 0 if solo event, -1 if unlimited
    /// </summary>
    public int MaxTeamMembers { get; set; } = 0;
}

public class CreateWeightEventDto : CreateEventDto {
    /// <summary>
    /// Data specific to the weight event
    /// </summary>
    public WeightEventData? Data { get; set; } = new();
}

public class CreateMedalEventDto : CreateEventDto {
    /// <summary>
    /// Data specific to the medal event
    /// </summary>
    public MedalEventData? Data { get; set; } = new();
}

public class CreatePestEventDto : CreateEventDto {
    /// <summary>
    /// Data specific to the pest event
    /// </summary>
    public PestEventData? Data { get; set; } = new();
}

public class CreateCollectionEventDto : CreateEventDto {
    /// <summary>
    /// Data specific to the pest event
    /// </summary>
    public CollectionEventData? Data { get; set; } = new();
}

public class CreateEventMemberDto  {
    public ulong EventId { get; set; }
    public EventType Type { get; set; }
    
    public Guid ProfileMemberId { get; set; }
    public double Score { get; set; }
    
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public ulong UserId { get; set; }
    
    public required ProfileMember ProfileMember { get; set; }
}