using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Events.DeleteEvent;

internal sealed class DeleteEventApprovalEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService)
	: Endpoint<EventIdRequest>
{
	public override void Configure() {
		Delete("/admin/events/{EventId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Delete Event";
		});
	}

	public override async Task HandleAsync(EventIdRequest request, CancellationToken c) 
	{
		var eliteEvent = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventIdUlong, cancellationToken: c);
		
		if (eliteEvent is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}
		
		if (eliteEvent.Banner is not null) {
			await objectStorageService.DeleteAsync(eliteEvent.Banner.Path, c);
		}

		context.Events.Remove(eliteEvent);
		await context.SaveChangesAsync(c);
		
		await SendNoContentAsync(cancellation: c);
	}
}