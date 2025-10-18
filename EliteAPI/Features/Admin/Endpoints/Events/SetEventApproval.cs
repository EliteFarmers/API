using System.ComponentModel;
using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Common;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Endpoints.Events;

public class SetEventApprovalRequest : EventIdRequest
{
	[QueryParam] [DefaultValue(false)] public bool? Approve { get; set; } = false;
}

internal sealed class SetEventApprovalEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore)
	: Endpoint<SetEventApprovalRequest>
{
	public override void Configure() {
		Post("/admin/events/{EventId}/approve");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(x => x.Accepts<SetEventApprovalRequest>());

		Summary(s => { s.Summary = "Set event approval"; });
	}

	public override async Task HandleAsync(SetEventApprovalRequest request, CancellationToken c) {
		var eliteEvent = await context.Events
			.Include(e => e.Banner)
			.FirstOrDefaultAsync(e => e.Id == request.EventIdUlong, c);

		if (eliteEvent is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		eliteEvent.Approved = request.Approve ?? false;
		await context.SaveChangesAsync(c);

		await cacheStore.EvictByTagAsync("upcoming-events", c);

		await Send.NoContentAsync(c);
	}
}

internal sealed class SetEventApprovalRequestValidator : Validator<SetEventApprovalRequest>
{
	public SetEventApprovalRequestValidator() {
		Include(new EventIdRequestValidator());
	}
}