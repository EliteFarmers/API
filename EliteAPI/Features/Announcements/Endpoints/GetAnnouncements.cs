using FastEndpoints;

namespace EliteAPI.Features.Announcements;

internal sealed class GetAnnouncementEndpoint(
    IAnnouncementService announcementService
) : EndpointWithoutRequest<List<AnnouncementDto>> 
{
    public override void Configure() {
        Get("/announcements");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "Get announcements";
            s.Description = "Gets all announcements that should be shown to users";
        });
        
        Options(o => {
            o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(30)).Tag("announcements"));
        });
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var announcements = await announcementService.GetAnnouncements(c);
        await SendAsync(announcements, cancellation: c);
    }
}