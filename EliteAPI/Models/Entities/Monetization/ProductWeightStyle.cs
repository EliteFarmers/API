using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Monetization;

[Table("ProductCosmetics")]
public class ProductWeightStyle {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	public ulong ProductId { get; set; }
	public int WeightStyleId { get; set; }
}