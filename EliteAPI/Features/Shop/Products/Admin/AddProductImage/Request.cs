using System.ComponentModel;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Shop.Products.Admin.AddProductImage;

internal sealed class AddProductImageRequest : DiscordIdRequest {
	[FromForm]
	public required UploadImageDto Image { get; set; }
	
	/// <summary>
	/// Use this to set the image as the product's thumbnail
	/// </summary>
	[QueryParam, DefaultValue(false)]
	public bool? Thumbnail { get; set; }
}

internal sealed class AddProductImageRequestValidator : Validator<AddProductImageRequest> {
	public AddProductImageRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}