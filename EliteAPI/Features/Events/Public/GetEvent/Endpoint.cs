using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Public.GetEvent;

internal sealed class GetEventRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetEventEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEventRequest, EventDetailsDto> {
	public override void Configure() {
		Get("/event/{EventId}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get an event"; });

		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(2))));
	}

	public override async Task HandleAsync(GetEventRequest request, CancellationToken c) {
		var eliteEvent = await context.Events.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.Approved, c);

		if (eliteEvent is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var mapped = mapper.Map<EventDetailsDto>(eliteEvent);

		if (mapped.Data is WeightEventData { CropWeights: not { Count: > 0 } } data)
			data.CropWeights = FarmingWeightConfig.Settings.EventCropWeights;

		var result = mapper.Map<EventDetailsDto>(eliteEvent);

		await Send.OkAsync(result, c);
	}
}