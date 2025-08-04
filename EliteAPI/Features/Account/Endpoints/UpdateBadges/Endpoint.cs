using EliteAPI.Data;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.UpdateBadges;

internal sealed class UpdateBadgesEndpoint(
	DataContext context
) : Endpoint<UpdateBadgesRequest> {
	
	public override void Configure() {
		Post("/account/{PlayerUuid}/badges");
		Version(0);

		Summary(s => {
			s.Summary = "Update Account Settings";
		});
	}

	public override async Task HandleAsync(UpdateBadgesRequest request, CancellationToken c) {
		var userId = User.GetDiscordId();
		if (userId is null) {
			await Send.UnauthorizedAsync(c);
			return;
		}
        
		var userBadges = await context.UserBadges
			.Include(a => a.MinecraftAccount)
			.Where(a => a.MinecraftAccountId == request.PlayerUuid && a.MinecraftAccount.AccountId == userId)
			.ToListAsync(cancellationToken: c);
        
		if (userBadges is { Count: 0 }) {
			await Send.NotFoundAsync(c);
		}

		foreach (var badge in request.Badges) {
			var existing = userBadges.FirstOrDefault(b => b.BadgeId == badge.BadgeId);
            
			if (existing is null) {
				AddError(r => r.Badges, $"Badge {badge.BadgeId} not found on account");
				continue;
			}
            
			existing.Visible = badge.Visible ?? existing.Visible;
			existing.Order = badge.Order ?? existing.Order;
		}
		
		ThrowIfAnyErrors();
        
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(cancellation: c);
	}
}