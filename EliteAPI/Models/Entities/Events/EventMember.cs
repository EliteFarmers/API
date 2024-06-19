using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events;

public enum EventMemberStatus {
	Inactive = 0,
	Active = 1,
	Left = 2,
	Disqualified = 3,
}

public class EventMember {
	[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	public EventMemberStatus Status { get; set; }
	public EventType Type { get; set; } = EventType.None;
    
	public double Score { get; set; }
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
	
	[ForeignKey("Team")]
	public int? TeamId { get; set; }
	public EventTeam? Team { get; set; }
}