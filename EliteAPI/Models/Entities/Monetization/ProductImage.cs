namespace EliteAPI.Models.Entities.Monetization;

public class ProductImage {
	public int Id { get; set; }
	public string? Title { get; set; }
	public string? Description { get; set; }
	public int Order { get; set; }
	public string Path { get; set; } = null!;
}