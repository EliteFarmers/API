using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Badges.UpdateBadge;

internal sealed class UpdateBadgeEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService,
	IOutputCacheStore cacheStore
	) : Endpoint<UpdateBadgeRequest> 
{
	public override void Configure() {
		Patch("/badge/{BadgeId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		AllowFileUploads();
		AllowFormData();

		Summary(s => {
			s.Summary = "Update a badge";
		});
	}

	public override async Task HandleAsync(UpdateBadgeRequest request, CancellationToken c) 
	{
		var existingBadge = await context.Badges
			.Include(b => b.Image)
			.FirstOrDefaultAsync(b => b.Id == request.BadgeId, cancellationToken: c);
    
		if (existingBadge is null) {
			await SendNotFoundAsync(c);
			return;
		}
    
		existingBadge.Name = request.Badge.Name ?? existingBadge.Name;
		existingBadge.Description = request.Badge.Description ?? existingBadge.Description;
		existingBadge.Requirements = request.Badge.Requirements ?? existingBadge.Requirements;
    
		if (request.Badge.Image is not null) {
			if (existingBadge.Image is not null) {
				await objectStorageService.DeleteAsync(existingBadge.Image.Path, c);
			}
        
			var image = await objectStorageService.UploadImageAsync($"badges/{existingBadge.Id}.png", request.Badge.Image, token: c);
       
			existingBadge.Image = image;
			existingBadge.ImageId = image.Id;
		}
    
		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("badges", c);
		
		await SendNoContentAsync(cancellation: c);
	}
}