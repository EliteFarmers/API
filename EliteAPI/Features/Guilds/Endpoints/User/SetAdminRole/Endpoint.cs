using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.SetAdminRole;

internal sealed class SetAdminRoleEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<SetAdminRoleRequest> {
	public override void Configure() {
		Put("/user/guild/{DiscordId}/adminrole");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute(GuildPermission.Admin)));
		Version(0);

		Summary(s => { s.Summary = "Set an admin role for a guild"; });
	}

	public override async Task HandleAsync(SetAdminRoleRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) ThrowError("User not found", StatusCodes.Status404NotFound);

		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		guild.AdminRole = request.RoleIdUlong;
		await context.SaveChangesAsync(c);

		await Send.NoContentAsync(c);
	}
}