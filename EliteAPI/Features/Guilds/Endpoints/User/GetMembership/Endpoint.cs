using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guilds.User.GetMembership;

internal sealed class GetUserGuildEndpoint(
	IDiscordService discordService,
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, AuthorizedGuildDto>
{
	public override void Configure() {
		Get("/user/guild/{DiscordId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Get a guild membership for the current user"; });
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) ThrowError("User not found", StatusCodes.Status404NotFound);

		var guildMember = await discordService.GetGuildMemberIfAdmin(User, request.DiscordIdUlong);

		if (guildMember is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var guild = await context.Guilds
			.Include(g => g.Roles)
			.Include(g => g.Channels).AsNoTracking()
			.FirstOrDefaultAsync(g => g.Id == request.DiscordIdUlong, c);

		await Send.OkAsync(new AuthorizedGuildDto {
			Id = request.DiscordIdUlong.ToString(),
			Permissions = guildMember.Permissions.ToString(),
			Guild = mapper.Map<PrivateGuildDto>(guild),
			Member = mapper.Map<GuildMemberDto>(guildMember)
		}, c);
	}
}