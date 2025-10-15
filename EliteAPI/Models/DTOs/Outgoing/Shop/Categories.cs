using System.ComponentModel.DataAnnotations;
using EliteAPI.Features.Account.DTOs;

namespace EliteAPI.Models.DTOs.Outgoing.Shop;

public class ShopCategoryDto
{
	public int Id { get; set; }
	public required string Title { get; set; }
	public required string Slug { get; set; }
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool Published { get; set; }
	public List<ProductDto> Products { get; set; } = [];
}

public class CreateCategoryDto
{
	[MaxLength(256)] public required string Title { get; set; }

	[MaxLength(32)] public required string Slug { get; set; }

	[MaxLength(512)] public string? Description { get; set; }
}

public class EditCategoryDto
{
	[MaxLength(256)] public string? Title { get; set; }

	[MaxLength(32)] public string? Slug { get; set; }

	[MaxLength(512)] public string? Description { get; set; }

	public bool? Published { get; set; }
}