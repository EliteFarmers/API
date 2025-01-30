using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.SetInvite;

internal sealed class SetInviteEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<SetInviteRequest> {
	
	public override void Configure() {
		Put("/user/guild/{DiscordId}/invite");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute(GuildPermission.Admin)));
		Version(0);

		Summary(s => {
			s.Summary = "Set invite code for a guild";
		});
		
	}

	public override async Task HandleAsync(SetInviteRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			ThrowError("User not found", StatusCodes.Status404NotFound);
		}

		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		guild.InviteCode = request.Invite;
        
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendNoContentAsync(c);
	}
}