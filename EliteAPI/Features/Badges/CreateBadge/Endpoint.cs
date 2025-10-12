using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Badges.CreateBadge;

internal sealed class CreateBadgeEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService,
	IOutputCacheStore cacheStore
) : Endpoint<CreateBadgeRequest> {
	public override void Configure() {
		Post("/badges");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		AllowFileUploads();
		AllowFormData();

		Description(d => d.AutoTagOverride("Badge"));

		Summary(s => { s.Summary = "Create a badge"; });
	}

	public override async Task HandleAsync(CreateBadgeRequest request, CancellationToken c) {
		var newBadge = new Badge {
			Name = request.Badge.Name,
			Description = request.Badge.Description,
			Requirements = request.Badge.Requirements,
			TieToAccount = request.Badge.TieToAccount
		};

		context.Badges.Add(newBadge);
		await context.SaveChangesAsync(c);

		if (request.Badge.Image is not null) {
			var image = await objectStorageService.UploadImageAsync($"badges/{newBadge.Id}.png", request.Badge.Image,
				token: c);

			newBadge.Image = image;
			newBadge.ImageId = image.Id;

			await context.SaveChangesAsync(c);
		}

		await cacheStore.EvictByTagAsync("badges", c);
		await Send.NoContentAsync(c);
	}
}