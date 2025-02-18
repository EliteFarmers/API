using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.SetEventBanner;

internal sealed class SetEventBannerRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
	[FromForm]
	public required EditEventBannerDto Data { get; set; }
}

internal sealed class SetEventBannerEndpoint(
	DataContext context,
	IObjectStorageService objectStorageService
) : Endpoint<SetEventBannerRequest> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/{EventId}/banner");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		
		AllowFileUploads();

		Summary(s => {
			s.Summary = "Set Custom Event Banner";
		});
	}

	public override async Task HandleAsync(SetEventBannerRequest request, CancellationToken c) {
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordIdUlong, cancellationToken: c);
        
        if (eliteEvent is null || eliteEvent.GuildId != request.DiscordIdUlong) {
	        await SendNotFoundAsync(c);
			return;
        }

        if (request.Data.Image is null) {
	        ThrowError("No image was provided.");
        }
        
        var newImage = await objectStorageService.UploadImageAsync(
	        path: $"guilds/{request.DiscordIdUlong}/events/{eliteEvent.Id}/banner.png", 
	        file: request.Data.Image, 
	        token: c
	    );
        
        if (eliteEvent.Banner is not null) {
	        eliteEvent.Banner.Metadata = newImage.Metadata;
	        eliteEvent.Banner.Hash = newImage.Hash;
	        eliteEvent.Banner.Title = newImage.Title;
	        eliteEvent.Banner.Description = newImage.Description;
        } else {
	        context.Images.Add(newImage);
	        eliteEvent.Banner = newImage;
        }
        
        await context.SaveChangesAsync(c);
        await SendOkAsync(cancellation: c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<SetEventBannerRequest> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}