using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guilds.Public.GetGuildEvents;

internal sealed class GetPublicGuildEventsEndpoint(
	DataContext context,
	IDiscordService discordService,
	AutoMapper.IMapper mapper
	) : Endpoint<DiscordIdRequest, List<EventDetailsDto>>
{
	public override void Configure() {
		Get("/guild/{DiscordId}/events");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get public guild";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(5)).Tag("guild-events"));
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null || !guild.IsPublic) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var events = await context.Events
			.Where(e => e.GuildId == guild.Id && e.Approved)
			.OrderBy(e => e.StartTime)
			.AsNoTracking()
			.ToListAsync(c);

		var mapped = mapper.Map<List<EventDetailsDto>>(events) ?? [];
		
		await SendAsync(mapped, cancellation: c);
	}
}