using System.Text.Json;
using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Events.Admin.CreateEvent;

internal sealed class CreateEventRequest : DiscordIdRequest
{
	[FastEndpoints.FromBody] public required CreateEventDto Event { get; set; }
}

internal sealed class CreateEventAdminEndpoint(
	IDiscordService discordService,
	IEventService eventService,
	AutoMapper.IMapper mapper
) : Endpoint<CreateEventRequest, EventDetailsDto>
{
	public override void Configure() {
		Post("/guild/{DiscordId}/events/weight");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Create Event"; });
	}

	public override async Task HandleAsync(CreateEventRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		if (!eventService.CanCreateEvent(guild, out var reason)) ThrowError(reason);

		request.Event.Type ??= EventType.FarmingWeight;
		var result = await eventService.CreateEvent(request.Event, request.DiscordIdUlong);

		if (result.Result is BadRequestObjectResult badRequest)
			ThrowError(badRequest.Value?.ToString() ?? "Failed to create event!");

		var mapped = mapper.Map<EventDetailsDto>(result.Value);
		await Send.OkAsync(mapped, c);
	}
}

internal sealed class CreateEventRequestValidator : Validator<CreateEventRequest>
{
	public CreateEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}