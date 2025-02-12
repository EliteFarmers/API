using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Events.SetEventApproval;

internal sealed class SetEventApprovalEndpoint(
	DataContext context)
	: Endpoint<SetEventApprovalRequest>
{
	public override void Configure() {
		Post("/admin/events/{EventId}/approve");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(e => e.ClearDefaultAccepts());
		
		Summary(s => {
			s.Summary = "Set event approval";
		});
	}

	public override async Task HandleAsync(SetEventApprovalRequest request, CancellationToken c) 
	{
		var eliteEvent = await context.Events
			.Include(e => e.Banner)
			.FirstOrDefaultAsync(e => e.Id == request.EventIdUlong, cancellationToken: c);

		if (eliteEvent is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}

		eliteEvent.Approved = request.Approve ?? false;
		await context.SaveChangesAsync(c);
		
		await SendOkAsync(cancellation: c);
	}
}