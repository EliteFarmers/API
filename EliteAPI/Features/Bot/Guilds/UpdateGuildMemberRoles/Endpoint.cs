using EliteAPI.Authentication;
using EliteAPI.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildMemberRoles;

internal sealed class UpdateGuildMemberRolesEndpoint(
	DataContext context
) : Endpoint<BotUpdateGuildMemberRolesRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}/members/{UserId}/roles");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Update Guild Member Roles";
		});
	}

	public override async Task HandleAsync(BotUpdateGuildMemberRolesRequest request, CancellationToken c) {
		var member = await context.GuildMembers
			.FirstOrDefaultAsync(m => m.GuildId == request.DiscordIdUlong && m.AccountId == request.UserId, cancellationToken: c);
        
		if (member is null) {
			await SendNotFoundAsync(c);
			return;
		}

		try {
			member.Roles = request.Roles.Select(ulong.Parse).ToList();
			member.LastUpdated = DateTimeOffset.UtcNow;
		} catch (Exception) {
			ThrowError("Failed to parse member roles");
		}
        
		context.GuildMembers.Update(member);
		await context.SaveChangesAsync(c);
		
		await SendOkAsync(cancellation: c);
	}
}