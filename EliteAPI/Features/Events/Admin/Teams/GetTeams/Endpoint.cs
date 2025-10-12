using System.Globalization;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.GetTeams;

internal sealed class GetTeamsRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetTeamsAdminEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper,
	IEventTeamService teamService
) : Endpoint<GetTeamsRequest, List<EventTeamWithMembersDto>> {
	public override void Configure() {
		Get("/guild/{DiscordId}/event/{EventId}/teams");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Get event teams"; });
	}

	public override async Task HandleAsync(GetTeamsRequest request, CancellationToken c) {
		var @event = await context.Events
			.Where(e => e.GuildId == request.DiscordIdUlong && e.Id == request.EventId)
			.AsNoTracking()
			.FirstOrDefaultAsync(c);

		if (@event is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var teams = await teamService.GetEventTeamsAsync(@event.Id);
		var result = teams.Select(t => new EventTeamWithMembersDto {
			EventId = t.EventId.ToString(),
			Id = t.Id,
			Name = t.Name,
			Score = t.Members.Sum(m => m.Score).ToString(CultureInfo.InvariantCulture),
			JoinCode = t.JoinCode,
			OwnerId = t.UserId,
			OwnerUuid = t.GetOwnerUuid(),
			Members = mapper.Map<List<EventMemberDto>>(t.Members)
		}).ToList();

		await Send.OkAsync(result, c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<GetTeamsRequest> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}