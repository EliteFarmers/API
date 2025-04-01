using EliteAPI.Authentication;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Events.Admin.CreateEventPests;

internal sealed class CreatePestsEventRequest : DiscordIdRequest {
	[FastEndpoints.FromBody]
	public required CreatePestEventDto Event { get; set; }
}

internal sealed class CreatePestsEventEndpoint(
	IDiscordService discordService,
	IEventService eventService,
	AutoMapper.IMapper mapper
) : Endpoint<CreatePestsEventRequest, EventDetailsDto> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/pests");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Create Pests Event";
		});
	}

	public override async Task HandleAsync(CreatePestsEventRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		if (!eventService.CanCreateEvent(guild, out var reason)) {
			ThrowError(reason);
		}

		request.Event.Type = EventType.Pests;
		var result = await eventService.CreateEvent(request.Event, request.DiscordIdUlong);

		if (result.Result is BadRequestObjectResult badRequest) {
			ThrowError(badRequest.Value?.ToString() ?? "Failed to create event!");
		}

		var mapped = mapper.Map<EventDetailsDto>(result.Value);
		await SendAsync(mapped, cancellation: c);
	}
}

internal sealed class CreatePestsEventRequestValidator : Validator<CreatePestsEventRequest> {
	public CreatePestsEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}