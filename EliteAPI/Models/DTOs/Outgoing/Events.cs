﻿using System.ComponentModel.DataAnnotations;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class EventDetailsDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
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
}

public class EventMemberDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    public required string EventId { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public string? AmountGained { get; set; }

    public StartConditions StartConditions { get; set; } = new();

    public string? LastUpdated { get; set; }
    
    public bool Disqualified { get; set; }
    [MaxLength(128)]
    public string? Notes { get; set; }
}

public class EventMemberDetailsDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    public required string EventId { get; set; }
    
    public EventMemberStatus Status { get; set; }
    public string? AmountGained { get; set; }
    public string? LastUpdated { get; set; }
}

public class EventMemberBannedDto {
    public string? PlayerUuid { get; set; }
    public string? PlayerName { get; set; }
    
    public string? AmountGained { get; set; }
    public string? Notes { get; set; }
    
    public string? LastUpdated { get; set; }
}

public class EditEventDto {
    public string? Name { get; set; }
    
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