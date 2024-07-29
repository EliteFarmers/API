using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Monetization;

public class WeightStyleImage {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[ForeignKey("WeightStyle")]
	public int WeightStyleId { get; set; }

	public WeightStyle WeightStyle { get; set; } = null!;
	public string? Url { get; set; }

	[MaxLength(64)]
	public string? Title { get; set; }

	[MaxLength(512)]
	public string? Description { get; set; }

	public int Order { get; set; }
}