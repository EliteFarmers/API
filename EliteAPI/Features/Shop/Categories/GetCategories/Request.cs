using FastEndpoints;

namespace EliteAPI.Features.Shop.Categories.GetCategories;

internal sealed class GetCategoriesRequest {
	/// <summary>
	/// Include products in response
	/// </summary>
	[QueryParam]
	public bool? IncludeProducts { get; set; }
}