using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Shop.Categories.ReorderCategoryProducts;

internal sealed class ReorderCategoryProductsRequest : ReorderStringRequest {
	/// <summary>
	/// Category id
	/// </summary>
	[RouteParam]
	public required int CategoryId { get; set; }
}

internal sealed class ReorderCategoryProductsRequestValidator : Validator<ReorderCategoryProductsRequest> {
	public ReorderCategoryProductsRequestValidator() {
		Include(new ReorderStringRequestValidator());
	}
}