using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Events;

public class EventTeam {
	[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[MaxLength(64)]
	public required string Name { get; set; }
	[MaxLength(7)]
	public string? Color { get; set; }
	[MaxLength(6)]
	public required string JoinCode { get; set; } = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..6];

	public List<EventMember> Members { get; set; } = [];
	
	public int OwnerId { get; set; }
	
	[ForeignKey("Event")]
	public int EventId { get; set; }
	public Event Event { get; set; } = null!;
}