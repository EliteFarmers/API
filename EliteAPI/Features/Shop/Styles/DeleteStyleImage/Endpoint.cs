using System.Web;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.DeleteStyleImage;

internal sealed class DeleteStyleImageRequest {
	public int StyleId { get; set; }
	public required string ImagePath { get; set; }
}

internal sealed class DeleteStyleImageEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IObjectStorageService objectStorageService
) : Endpoint<DeleteStyleImageRequest> {
	
	public override void Configure() {
		Delete("/product/style/{StyleId}/images/{ImagePath}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Remove Image from Style";
		});
	}

	public override async Task HandleAsync(DeleteStyleImageRequest request, CancellationToken c) {
		var style = await context.WeightStyles
			.FirstOrDefaultAsync(s => s.Id == request.StyleId, c);
		
		if (style is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var decoded = HttpUtility.UrlDecode(request.ImagePath);
		var styleImage = style.Images.FirstOrDefault(i => decoded.EndsWith(i.Path))
		                 ?? (style.Image?.Path == decoded ? style.Image : null);
		
		if (styleImage is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		if (style.Image == styleImage) {
			style.Image = null;
		} else {
			style.Images.Remove(styleImage);
		}
		
		await context.SaveChangesAsync(c);
		await objectStorageService.DeleteAsync(styleImage.Path, c);
		
		await cacheStore.EvictByTagAsync("styles", c);

		await SendOkAsync(cancellation: c);
	}
}