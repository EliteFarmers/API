using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.DeleteEvent;

internal sealed class DeleteEventRequest : DiscordIdRequest
{
	public ulong EventId { get; set; }
}

internal sealed class DeleteEventAdminEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService
) : Endpoint<DeleteEventRequest>
{
	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute(GuildPermission.Admin)));
		Version(0);

		Summary(s => {
			s.Summary = "Delete Event";
			s.Description = "Delete an event and all associated data. Only available for unapproved events.";
		});
	}

	public override async Task HandleAsync(DeleteEventRequest request, CancellationToken c) {
		var eliteEvent = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordIdUlong, c);

		if (eliteEvent is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		if (eliteEvent.Approved) ThrowError("You cannot delete an approved event.");

		if (eliteEvent.Banner is not null) {
			await objectStorageService.DeleteAsync(eliteEvent.Banner.Path, c);

			context.Images.Remove(eliteEvent.Banner);
			eliteEvent.Banner = null;
			eliteEvent.BannerId = null;
		}

		context.Events.Remove(eliteEvent);
		await context.SaveChangesAsync(c);

		await Send.NoContentAsync(c);
	}
}

internal sealed class DeleteEventRequestValidator : Validator<DeleteEventRequest>
{
	public DeleteEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}