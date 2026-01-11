using System.Net;
using EliteAPI.Features.Guides.Endpoints;
using EliteAPI.Features.Guides.Models;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class AdminGuidesTests(GuideTestApp App) : TestBase
{
    [Fact]
    public async Task GetPendingGuides_AsModerator_ReturnsPendingGuides()
    {
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();

        var submitRsp = await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        submitRsp.IsSuccessStatusCode.ShouldBeTrue();

        var (pendingRsp, pendingGuides) = await App.ModeratorClient.GETAsync<AdminPendingGuidesEndpoint, List<GuideResponse>>();
        
        pendingRsp.IsSuccessStatusCode.ShouldBeTrue();
        pendingGuides.ShouldNotBeNull();
        pendingGuides.ShouldContain(g => g.Id == created.Id);
    }

    [Fact]
    public async Task GetPendingGuides_DoesNotShowDrafts()
    {
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        var (pendingRsp, pendingGuides) = await App.ModeratorClient.GETAsync<AdminPendingGuidesEndpoint, List<GuideResponse>>();
        
        pendingRsp.IsSuccessStatusCode.ShouldBeTrue();
        pendingGuides.ShouldNotContain(g => g.Id == created!.Id);
    }
    protected override async ValueTask TearDownAsync()
    {
        await App.CleanUpGuidesAsync();
    }
}
