using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.DeleteStyle;

internal sealed class DeleteStyleRequest {
	public int StyleId { get; set; }
}

internal sealed class DeleteStyleEndpoint(
	DataContext context,
	IOutputCacheStore outputCacheStore
) : Endpoint<DeleteStyleRequest> {
	public override void Configure() {
		Delete("/product/style/{StyleId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => { s.Summary = "Delete Shop Style"; });
	}

	public override async Task HandleAsync(DeleteStyleRequest request, CancellationToken c) {
		var existing = await context.WeightStyles
			.FirstOrDefaultAsync(s => s.Id == request.StyleId, c);

		if (existing is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		context.WeightStyles.Remove(existing);
		await context.SaveChangesAsync(c);

		await outputCacheStore.EvictByTagAsync("styles", c);

		await Send.NoContentAsync(c);
	}
}