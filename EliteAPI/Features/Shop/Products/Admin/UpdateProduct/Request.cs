using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Shop.Products.Admin.UpdateProduct;

internal sealed class UpdateProductRequest : DiscordIdRequest {
	[FromBody]
	public required EditProductDto ProductData { get; set; }
}

internal sealed class UpdateProductRequestValidator : Validator<UpdateProductRequest> {
	public UpdateProductRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}