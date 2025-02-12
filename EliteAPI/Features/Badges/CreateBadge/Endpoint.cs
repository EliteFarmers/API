using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Badges.CreateBadge;

internal sealed class CreateBadgeEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService,
	IOutputCacheStore cacheStore
	) : Endpoint<CreateBadgeRequest> 
{
	public override void Configure() {
		Post("/badge/{BadgeId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		AllowFileUploads();
		AllowFormData();

		Summary(s => {
			s.Summary = "Create a badge";
		});
	}

	public override async Task HandleAsync(CreateBadgeRequest request, CancellationToken c) 
	{
		var newBadge = new Badge {
			Name = request.Name,
			Description = request.Description,
			Requirements = request.Requirements,
			TieToAccount = request.TieToAccount
		};
        
		context.Badges.Add(newBadge);
		await context.SaveChangesAsync(c);
        
		if (request.Image is not null) {
			var image = await objectStorageService.UploadImageAsync($"badges/{newBadge.Id}.png", request.Image, token: c);
           
			newBadge.Image = image;
			newBadge.ImageId = image.Id;
            
			await context.SaveChangesAsync(c);
		}
		
		await cacheStore.EvictByTagAsync("badges", c);
		await SendOkAsync(cancellation: c);
	}
}