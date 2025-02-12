using System.ComponentModel;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.AddStyleImage;

internal sealed class Request {
	public int StyleId { get; set; }
	
	[FromForm]
	public required UploadImageDto Image { get; set; }
	
	/// <summary>
	/// Use this to set the image as the product's thumbnail
	/// </summary>
	[QueryParam, DefaultValue(false)]
	public bool? Thumbnail { get; set; }
}

internal sealed class AddStyleImageEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IObjectStorageService objectStorageService
) : Endpoint<Request> {
	
	public override void Configure() {
		Post("/product/style/{StyleId}/images");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		AllowFileUploads();

		Summary(s => {
			s.Summary = "Add Image To Style";
		});
	}

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var style = await context.WeightStyles
			.FirstOrDefaultAsync(s => s.Id == request.StyleId, c);
		
		if (style is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var image = await objectStorageService.UploadImageAsync(
			path: $"cosmetics/weightstyles/{style.Id}/{Guid.NewGuid()}.png",
			file: request.Image.Image, 
			token: c
		);
		
		image.Title = request.Image.Title;
		image.Description = request.Image.Description;

		if (request.Thumbnail is true) {
			if (style.Image is not null) {
				await objectStorageService.DeleteAsync(style.Image.Path, c);
				style.Images.Remove(style.Image);
			}
			
			style.Image = image;
		} else {
			style.Images.Add(image);
		}
		
		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("styles", c);

		await SendOkAsync(cancellation: c);
	}
}