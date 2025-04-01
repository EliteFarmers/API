using EliteAPI.Authentication;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Events.Admin.CreateEventWeight;

internal sealed class CreateWeightEventRequest : DiscordIdRequest {
	[FastEndpoints.FromBody]
	public required CreateWeightEventDto Event { get; set; }
}

internal sealed class CreateWeightEventEndpoint(
	IDiscordService discordService,
	IEventService eventService,
	AutoMapper.IMapper mapper
) : Endpoint<CreateWeightEventRequest, EventDetailsDto> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/weight");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Create Weight Event";
		});
	}

	public override async Task HandleAsync(CreateWeightEventRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		if (!eventService.CanCreateEvent(guild, out var reason)) {
			ThrowError(reason);
		}

		request.Event.Type = EventType.FarmingWeight;
		var result = await eventService.CreateEvent(request.Event, request.DiscordIdUlong);

		if (result.Result is BadRequestObjectResult badRequest) {
			ThrowError(badRequest.Value?.ToString() ?? "Failed to create event!");
		}

		var mapped = mapper.Map<EventDetailsDto>(result.Value);
		await SendAsync(mapped, cancellation: c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<CreateWeightEventRequest> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}