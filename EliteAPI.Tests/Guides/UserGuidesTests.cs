using System.Net;
using EliteAPI.Features.Guides.Endpoints;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Models.Dtos;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class UserGuidesTests(GuideTestApp App) : TestBase
{
    [Fact]
    public async Task GetUserGuides_ReturnsOwnGuides_OfAllStatuses()
    {
        // Create a Draft guide
        var (rsp1, draft) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        rsp1.IsSuccessStatusCode.ShouldBeTrue($"Draft creation failed: {rsp1.StatusCode}");

        // Create another guide and Submit it (Pending)
        var (rsp2, pending) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        rsp2.IsSuccessStatusCode.ShouldBeTrue($"Pending guide creation failed: {rsp2.StatusCode}");

        var submitRsp1 = await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = pending!.Id });
        submitRsp1.IsSuccessStatusCode.ShouldBeTrue($"Pending guide submission failed: {submitRsp1.StatusCode}");

        var (rsp3, published) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        rsp3.IsSuccessStatusCode.ShouldBeTrue($"Published guide creation failed: {rsp3.StatusCode}");

        var submitRsp2 = await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = published!.Id });
        submitRsp2.IsSuccessStatusCode.ShouldBeTrue($"Published guide submission failed: {submitRsp2.StatusCode}");

        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = published.Id });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue($"Published guide approval failed: {approveRsp.StatusCode}");

        // Retrieve user guides
        var result = await App.RegularUserClient.GETAsync<GetUserGuidesEndpoint, GetUserGuidesRequest, List<UserGuideDto>>(
            new GetUserGuidesRequest { AccountId = GuideTestApp.RegularUserId });

        result.Response.IsSuccessStatusCode.ShouldBeTrue($"Get user guides failed: {result.Response.StatusCode}");
        var guides = result.Result;
        
        // Verify all statuses are present
        guides.ShouldContain(g => g.Id == draft!.Id);
        guides.ShouldContain(g => g.Id == pending.Id);
        guides.ShouldContain(g => g.Id == published.Id);
        
        // Verify statuses in response match
        guides.First(g => g.Id == draft!.Id).Status.ShouldBe(GuideStatus.Draft.ToString());
        guides.First(g => g.Id == pending.Id).Status.ShouldBe(GuideStatus.PendingApproval.ToString());
        guides.First(g => g.Id == published.Id).Status.ShouldBe(GuideStatus.Published.ToString());
    }

    [Fact]
    public async Task GetUserGuides_DoesNotReturnOtherUsersGuides()
    {
        // Create a guide as Regular User
        var (rsp1, userGuide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        rsp1.IsSuccessStatusCode.ShouldBeTrue();

        // Create a guide as Moderator
        var (rsp2, modGuide) = await App.ModeratorClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        rsp2.IsSuccessStatusCode.ShouldBeTrue();

        // Get Regular User's guides
        var result = await App.RegularUserClient.GETAsync<GetUserGuidesEndpoint, GetUserGuidesRequest, List<UserGuideDto>>(
            new GetUserGuidesRequest { AccountId = GuideTestApp.RegularUserId });
        
        result.Response.IsSuccessStatusCode.ShouldBeTrue();
        var guides = result.Result;

        // Verify: Contains userGuide, Does NOT contain modGuide
        guides.ShouldContain(g => g.Id == userGuide!.Id);
        
        // Ensure modGuide is not in the list (assuming IDs are unique globally)
        guides.ShouldNotContain(g => g.Id == modGuide!.Id);
    }
    protected override async ValueTask TearDownAsync()
    {
        await App.CleanUpGuidesAsync();
    }
}
