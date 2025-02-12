using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Shop.Products.Admin.DeleteProductImage;

internal sealed class DeleteProductImageRequest : DiscordIdRequest {
	public required string ImagePath { get; set; }
}

internal sealed class DeleteProductImageRequestValidator : Validator<DeleteProductImageRequest> {
	public DeleteProductImageRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}