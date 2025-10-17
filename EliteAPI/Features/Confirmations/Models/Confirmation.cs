namespace EliteAPI.Features.Confirmations.Models;

public class Confirmation
{
	public int Id { get; set; }
	public string? Title { get; set; }
	public string? Content { get; set; }
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public ICollection<UserConfirmation> UserConfirmations { get; set; } = [];
}