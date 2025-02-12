using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Events.GetPendingEvents;

internal sealed class GetPendingEventsEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper)
	: EndpointWithoutRequest<List<EventDetailsDto>> 
{
	public override void Configure() {
		Get("/admin/events/pending");
		Policies(ApiUserPolicies.Moderator);
		Version(0);
		
		Summary(s => {
			s.Summary = "Get events pending approval";
		});
	}

	public override async Task HandleAsync(CancellationToken c) 
	{
		var events = await context.Events
			.Where(e => !e.Approved)
			.ToListAsync(cancellationToken: c);
        
		var result = mapper.Map<List<EventDetailsDto>>(events);
		
		await SendAsync(result, cancellation: c);
	}
}