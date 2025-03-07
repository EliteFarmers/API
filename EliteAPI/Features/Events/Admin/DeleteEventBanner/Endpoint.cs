using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.DeleteEventBanner;

internal sealed class DeleteEventBannerRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
}

internal sealed class DeleteEventBannerEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService
) : Endpoint<DeleteEventBannerRequest> {

	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}/banner");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		
		Summary(s => {
			s.Summary = "Delete Custom Event Banner";
		});
	}

	public override async Task HandleAsync(DeleteEventBannerRequest request, CancellationToken c) {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordIdUlong, cancellationToken: c);

        if (eliteEvent?.Banner is null) {
	        await SendNoContentAsync(cancellation: c);
	        return;
        }
        
        await objectStorageService.DeleteAsync(eliteEvent.Banner.Path, c);
        context.Images.Remove(eliteEvent.Banner);
        eliteEvent.Banner = null;
        eliteEvent.BannerId = null;
            
        await context.SaveChangesAsync(c);
        await SendNoContentAsync(cancellation: c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<DeleteEventBannerRequest> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}