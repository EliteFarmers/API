using System.Web;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Shop.Products.Admin.DeleteProductImage;

internal sealed class DeleteProductImageEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IConnectionMultiplexer redis,
	IObjectStorageService objectStorageService
) : Endpoint<DeleteProductImageRequest> {
	
	public override void Configure() {
		Delete("/product/{DiscordId}/images/{ImagePath}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Remove Image from Product";
		});
	}

	public override async Task HandleAsync(DeleteProductImageRequest request, CancellationToken c) {
		var product = await context.Products.FirstOrDefaultAsync(p => p.Id == request.DiscordIdUlong, cancellationToken: c);
		if (product is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}
		
		var decoded = HttpUtility.UrlDecode(request.ImagePath);
		var productImage = product.Images.FirstOrDefault(i => decoded.EndsWith(i.Path))
		                   ?? (product.Thumbnail?.Path == decoded ? product.Thumbnail : null);
		
		if (productImage is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}
		
		if (product.Thumbnail == productImage) {
			product.Thumbnail = null;
			product.ThumbnailId = null;
		} else {
			product.Images.Remove(productImage);
		}
		
		await context.SaveChangesAsync(c);
		await objectStorageService.DeleteAsync(productImage.Path, c);

		// Clear the product list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:productlist");
		
		await cacheStore.EvictByTagAsync("products", c);

		await SendOkAsync(cancellation: c);
	}
}