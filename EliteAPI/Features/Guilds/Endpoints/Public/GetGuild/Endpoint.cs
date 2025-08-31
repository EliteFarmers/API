using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Public.GetGuild;

internal sealed class GetPublicGuildEndpoint(
	IDiscordService discordService,
	AutoMapper.IMapper mapper
	) : Endpoint<DiscordIdRequest, PublicGuildDto>
{
	public override void Configure() {
		Get("/guild/{DiscordId}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get public guild";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(2)).Tag("guild"));
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null || !guild.IsPublic) {
			await Send.NotFoundAsync(c);
			return;
		}
        
		var mapped = mapper.Map<PublicGuildDto>(guild);
		await Send.OkAsync(mapped, cancellation: c);
	}
}