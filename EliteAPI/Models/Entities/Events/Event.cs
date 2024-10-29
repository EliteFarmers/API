using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Events;

public enum EventType {
    None = 0,
    FarmingWeight = 1,
    Collection = 2,
    Experience = 3,
    Medals = 4,
}

public class Event 
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    public bool Public { get; set; }
    public EventType Type { get; set; } = EventType.None;
    
    [MaxLength(64)]
    public required string Name { get; set; }
    
    public bool Approved { get; set; }
    
    [MaxLength(1024)]
    public string? Description { get; set; }
    [MaxLength(1024)]
    public string? Rules { get; set; }
    [MaxLength(1024)]
    public string? PrizeInfo { get; set; }
    
    public Image? Banner { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset JoinUntilTime { get; set; }
    
    public bool DynamicStartTime { get; set; }
    public bool Active { get; set; }
    public int MaxTeams { get; set; }
    public int MaxTeamMembers { get; set; }
    
    public List<EventTeam> Teams { get; set; } = [];
    
    [MaxLength(24)]
    public string? RequiredRole { get; set; }
    [MaxLength(24)]
    public string? BlockedRole { get; set; }
    
    [ForeignKey("Guild")]
    public ulong GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
}

public class EventEntityConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Navigation(e => e.Banner).AutoInclude();
        
        builder.HasDiscriminator(e => e.Type)
            .HasValue<Event>(EventType.None)
            .HasValue<WeightEvent>(EventType.FarmingWeight)
            .HasValue<MedalEvent>(EventType.Medals);
    }
}

public static class EventTeamMode {
    public const string Solo = "solo";
    public const string Teams = "teams";
    public const string CustomTeams = "custom";
    
    public static string GetMode(this Event @event) {
        if (@event.MaxTeams != 0) return Teams;
        if (@event.MaxTeamMembers != 0) return CustomTeams;
        return Solo;
    }
}