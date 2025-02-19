using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.CreateTeam;

internal sealed class CreateTeamRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
	[FastEndpoints.FromBody]
	public required CreateEventTeamDto Team { get; set; }
}

internal sealed class CreateTeamEndpoint(
	IEventTeamService teamService,
	DataContext context
) : Endpoint<CreateTeamRequest> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/{EventId}/teams");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Create an Event Team";
			s.Description =
				"This generally should only be used for events with a set amount of teams (when users are not allowed to create their own teams)";
		});
	}

	public override async Task HandleAsync(CreateTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordIdUlong, cancellationToken: c);
		
		if (userId is null || @event is null) {
			await SendUnauthorizedAsync(c);
			return;
		}
		
		var result = await teamService.CreateAdminTeamAsync(request.EventId, request.Team, userId);
		
		if (result is BadRequestObjectResult badRequest) {
			await SendAsync(badRequest.Value?.ToString(), cancellation: c);
			return;
		}

		await SendNoContentAsync(cancellation: c);
	}
}

internal sealed class CreateTeamRequestValidator : Validator<CreateTeamRequest> {
	public CreateTeamRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}