using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.UpdateEvent;

internal sealed class UpdateEventRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
	[FromBody] public required EditEventDto Event { get; set; }
}

internal sealed class UpdateEventAdminEndpoint(
	DataContext context
) : Endpoint<UpdateEventRequest, EventDetailsDto> {
	public override void Configure() {
		Patch("/guild/{DiscordId}/events/{EventId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Update Event"; });
	}

	public override async Task HandleAsync(UpdateEventRequest request, CancellationToken c) {
		var eliteEvent = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordIdUlong, c);

		if (eliteEvent is null || eliteEvent.GuildId != request.DiscordIdUlong) {
			await Send.NotFoundAsync(c);
			return;
		}

		var incoming = request.Event;
		var startTime = incoming.StartTime is not null
			? DateTimeOffset.FromUnixTimeSeconds(incoming.StartTime.Value)
			: (DateTimeOffset?)null;
		var endTime = incoming.EndTime is not null
			? DateTimeOffset.FromUnixTimeSeconds(incoming.EndTime.Value)
			: (DateTimeOffset?)null;
		var joinTime = incoming.JoinTime is not null
			? DateTimeOffset.FromUnixTimeSeconds(incoming.JoinTime.Value)
			: (DateTimeOffset?)null;

		eliteEvent.Name = incoming.Name ?? eliteEvent.Name;
		eliteEvent.Description = incoming.Description ?? eliteEvent.Description;
		eliteEvent.JoinUntilTime = joinTime ?? eliteEvent.JoinUntilTime;
		eliteEvent.DynamicStartTime = incoming.DynamicStartTime ?? eliteEvent.DynamicStartTime;
		eliteEvent.Active = incoming.Active ?? eliteEvent.Active;
		eliteEvent.Rules = incoming.Rules ?? eliteEvent.Rules;
		eliteEvent.PrizeInfo = incoming.PrizeInfo ?? eliteEvent.PrizeInfo;
		eliteEvent.RequiredRole = incoming.RequiredRole ?? eliteEvent.RequiredRole;
		eliteEvent.BlockedRole = incoming.BlockedRole ?? eliteEvent.BlockedRole;

		switch (eliteEvent) {
			case MedalEvent medalEvent when incoming.MedalData is not null:
				medalEvent.Data = incoming.MedalData;
				break;
			case WeightEvent weightEvent when incoming.WeightData is not null:
				weightEvent.Data = incoming.WeightData;
				break;
			case PestEvent pestEvent when incoming.PestData is not null:
				pestEvent.Data = incoming.PestData;
				break;
			case CollectionEvent collectionEvent when incoming.CollectionData is not null:
				collectionEvent.Data = incoming.CollectionData;
				break;
		}

		// Update all related event members if the start or end time has changed
		var updateStart = startTime is not null && startTime != eliteEvent.StartTime;
		var updateEnd = endTime is not null && endTime != eliteEvent.EndTime;

		eliteEvent.StartTime = startTime ?? eliteEvent.StartTime;
		eliteEvent.EndTime = endTime ?? eliteEvent.EndTime;

		await context.SaveChangesAsync(c);

		if (updateStart || updateEnd)
			await context.EventMembers
				.Where(em => em.EventId == eliteEvent.Id)
				.ExecuteUpdateAsync(m => m
					.SetProperty(e => e.StartTime, eliteEvent.StartTime)
					.SetProperty(e => e.EndTime, eliteEvent.EndTime), c);

		await Send.NoContentAsync(c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<UpdateEventRequest> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}