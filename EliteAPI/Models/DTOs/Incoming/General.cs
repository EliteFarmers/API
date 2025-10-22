namespace EliteAPI.Models.DTOs.Incoming;

public class ReorderElement<T>
{
	public required T Id { get; set; }
	public int Order { get; set; }
}