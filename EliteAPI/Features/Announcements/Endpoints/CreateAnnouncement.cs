using EliteAPI.Features.Auth;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;

namespace EliteAPI.Features.Announcements;

internal sealed class CreateAnnouncementEndpoint(
    IAnnouncementService announcementService
) : Endpoint<CreateAnnouncementDto> 
{
    public override void Configure() {
        Post("/announcements/create");
        Policies(ApiUserPolicies.Admin);
        Version(0);
        
        Description(x => x.Accepts<CreateAnnouncementDto>());

        Summary(s => {
            s.Summary = "Create an announcement";
            s.Description = "Creates a new announcement that will be displayed to users";
        });
    }

    public override async Task HandleAsync(CreateAnnouncementDto request, CancellationToken c)
    {
        await announcementService.CreateAnnouncementAsync(request, c);
		
        await SendNoContentAsync(c);
    }
}