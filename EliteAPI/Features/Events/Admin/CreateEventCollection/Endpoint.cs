using EliteAPI.Authentication;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Events.Admin.CreateEventCollection;

internal sealed class CreateCollectionEventRequest : DiscordIdRequest {
	[FastEndpoints.FromBody]
	public required CreateCollectionEventDto Event { get; set; }
}

internal sealed class CreateCollectionEventEndpoint(
	IDiscordService discordService,
	IEventService eventService,
	AutoMapper.IMapper mapper
) : Endpoint<CreateCollectionEventRequest, EventDetailsDto> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/collection");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Create Collection Event";
		});
	}

	public override async Task HandleAsync(CreateCollectionEventRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		if (!eventService.CanCreateEvent(guild, out var reason)) {
			ThrowError(reason);
		}

		request.Event.Type = EventType.Collection;
		var result = await eventService.CreateEvent(request.Event, request.DiscordIdUlong);

		if (result.Result is BadRequestObjectResult badRequest) {
			ThrowError(badRequest.Value?.ToString() ?? "Failed to create event!");
		}

		var mapped = mapper.Map<EventDetailsDto>(result.Value);
		await SendAsync(mapped, cancellation: c);
	}
}

internal sealed class CreateCollectionEventRequestValidator : Validator<CreateCollectionEventRequest> {
	public CreateCollectionEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}