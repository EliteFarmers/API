using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.DeleteStyle;

internal sealed class Request {
	public int StyleId { get; set; }
}

internal sealed class DeleteStyleEndpoint(
	DataContext context,
	IOutputCacheStore outputCacheStore
) : Endpoint<Request> {
	
	public override void Configure() {
		Delete("/product/style/{StyleId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Delete Shop Style";
		});
	}

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var existing = await context.WeightStyles
			.FirstOrDefaultAsync(s => s.Id == request.StyleId, c);
		
		if (existing is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		context.WeightStyles.Remove(existing);
		await context.SaveChangesAsync(c);
		
		await outputCacheStore.EvictByTagAsync("styles", c);

		await SendOkAsync(cancellation: c);
	}
}