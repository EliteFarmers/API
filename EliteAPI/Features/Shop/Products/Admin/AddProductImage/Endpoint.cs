using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Shop.Products.Admin.AddProductImage;

internal sealed class AddProductImageEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IConnectionMultiplexer redis,
	IObjectStorageService objectStorageService
) : Endpoint<AddProductImageRequest> {
	
	public override void Configure() {
		Post("/product/{DiscordId}/images");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		AllowFileUploads();

		Summary(s => {
			s.Summary = "Add Image To Product";
		});
	}

	public override async Task HandleAsync(AddProductImageRequest request, CancellationToken c) {
		var product = await context.Products.FirstOrDefaultAsync(p => p.Id == request.DiscordIdUlong, cancellationToken: c);
		if (product is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}
		
		var image = await objectStorageService.UploadImageAsync($"products/{request.DiscordIdUlong}/{Guid.NewGuid()}.png", request.Image.Image, token: c);
		
		image.Title = request.Image.Title;
		image.Description = request.Image.Description;

		if (request.Thumbnail is true) {
			if (product.Thumbnail is not null) {
				await objectStorageService.DeleteAsync(product.Thumbnail.Path, c);
				
				product.Thumbnail = null;
				product.ThumbnailId = null;
				
				await context.SaveChangesAsync(c);
			}
			
			product.Thumbnail = image;
			product.ThumbnailId = image.Id;
		} else {
			product.Images.Add(image);
		}
		
		await context.SaveChangesAsync(c);
		
		// Clear the product list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:productlist");
		
		await cacheStore.EvictByTagAsync("products", c);

		await SendOkAsync(cancellation: c);
	}
}