using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Badges.DeleteBadge;

internal sealed class DeleteBadgeEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService,
	IOutputCacheStore cacheStore
	) : Endpoint<BadgeRequest> 
{
	public override void Configure() {
		Delete("/badge/{BadgeId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Delete a badge";
		});
	}

	public override async Task HandleAsync(BadgeRequest request, CancellationToken c) 
	{
		var existingBadge = await context.Badges
			.Include(b => b.Image)
			.FirstOrDefaultAsync(b => b.Id == request.BadgeId, cancellationToken: c);
    
		if (existingBadge is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		context.Badges.Remove(existingBadge);
		
		if (existingBadge.Image is not null) {
			await objectStorageService.DeleteAsync(existingBadge.Image.Path, c);
		}
		
		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("badges", c);
		
		await Send.NoContentAsync(cancellation: c);
	}
}