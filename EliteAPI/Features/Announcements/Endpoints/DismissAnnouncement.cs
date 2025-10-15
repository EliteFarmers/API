using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Announcements;

internal sealed class DismissAnnouncementDto
{
	public Guid AnnouncementId { get; set; } = Guid.Empty;
}

internal sealed class DismissAnnouncementEndpoint(
	IAnnouncementService announcementService
) : Endpoint<DismissAnnouncementDto, ErrorOr<Success>>
{
	public override void Configure() {
		Post("/announcements/{AnnouncementId}/dismiss");
		Version(0);

		Description(x => x.Accepts<DismissAnnouncementDto>());

		Summary(s => {
			s.Summary = "Dismiss an announcement";
			s.Description = "Mark an announcement as dismissed for the current user";
		});
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(DismissAnnouncementDto request, CancellationToken c) {
		var userId = User.GetDiscordId();
		if (userId is null) return Error.Unauthorized();

		return await announcementService.DismissAnnouncementAsync(request.AnnouncementId, userId.Value, c);
	}
}