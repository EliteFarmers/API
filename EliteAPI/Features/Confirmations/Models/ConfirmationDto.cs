namespace EliteAPI.Features.Confirmations.Models;

public class ConfirmationDto
{
	public int Id { get; set; }
	public string? Title { get; set; }
	public string? Content { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
}