using System.ComponentModel;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Events.Public.GetUpcoming;

internal sealed class GetUpcomingEventsRequest {
	/// <summary>
	/// Offset by an amount of days to also include recently ended events.
	/// </summary>
	[QueryParam]
	[DefaultValue(0)]
	public int? Offset { get; set; } = 0;
}

internal sealed class GetUpcomingEventsEndpoint(
	IEventService eventService)
	: Endpoint<GetUpcomingEventsRequest, List<EventDetailsDto>> {
	public override void Configure() {
		Get("/events");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get upcoming events"; });

		Description(d => {
			d.Accepts<GetUpcomingEventsRequest>();
			d.AutoTagOverride("Event");
		});

		Options(opt => opt.CacheOutput(o => o
			.Expire(TimeSpan.FromMinutes(10))
			.SetVaryByQuery(["offset"])
			.Tag("upcoming-events"))
		);
	}

	public override async Task HandleAsync(GetUpcomingEventsRequest request, CancellationToken c) {
		var result = await eventService.GetUpcomingEvents(-(request.Offset ?? 0));

		await Send.OkAsync(result, c);
	}
}