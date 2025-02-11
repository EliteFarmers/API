using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Events.Public.GetUpcoming;

internal sealed class GetUpcomingEventsEndpoint(
	IEventService eventService)
	: EndpointWithoutRequest<List<EventDetailsDto>>
{
	public override void Configure() {
		Get("/events");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get upcoming events";
		});
		
		Description(d => d.AutoTagOverride("Event"));
		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(10))));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = await eventService.GetUpcomingEvents();

		await SendAsync(result, cancellation: c);
	}
}