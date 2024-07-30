using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Events;

public class EventTeam {
	[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[MaxLength(64)]
	public required string Name { get; set; }
	[MaxLength(6)]
	public string? Color { get; set; }

	[MaxLength(6)] 
	public string JoinCode { get; set; } = NewJoinCode();

	public List<EventMember> Members { get; set; } = [];
	public double Score => Members.Sum(m => m.Score);
	
	[MaxLength(22)]
	public required string UserId { get; set; }
	
	[ForeignKey("Event")]
	public ulong EventId { get; set; }
	public Event Event { get; set; } = null!;
	
	public static string NewJoinCode() {
		return Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..6].ToUpperInvariant();
	}

	public string GetOwnerUuid() {
		return Members.Find(m => UserId == m.UserId.ToString())?.ProfileMember?.PlayerUuid ?? string.Empty;
	}
}