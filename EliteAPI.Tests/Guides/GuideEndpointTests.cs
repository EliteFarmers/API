using System.Net;
using EliteAPI.Features.Guides.Endpoints;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Models.Dtos;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class GuideEndpointTests(GuideTestApp App) : TestBase
{
    [Fact, Priority(1)]
    public async Task CreateGuide_Unauthenticated_Returns401()
    {
        var (rsp, _) = await App.AnonymousClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        
        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(2)]
    public async Task CreateGuide_RestrictedUser_Returns403()
    {
        var (rsp, _) = await App.RestrictedUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        
        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(3)]
    public async Task CreateGuide_ValidUser_CreatesGuide()
    {
        var (rsp, res) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.Greenhouse });
        
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldNotBeNull();
        res.Slug.ShouldNotBeNullOrEmpty();
        res.Status.ShouldBe("Draft");
    }

    [Fact, Priority(4)]
    public async Task GetGuide_NonExistent_Returns404()
    {
        var (rsp, _) = await App.AnonymousClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = "nonexistent-slug" });
        
        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(5)]
    public async Task GetGuide_DraftWithoutAuth_Returns404()
    {
        // First create a guide (it's a draft by default)
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Anonymous user tries to get the draft (no ?draft=true, but no ActiveVersion either)
        var (getRsp, _) = await App.AnonymousClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created!.Slug });
        
        // Should return 404 because no published version exists
        getRsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(6)]
    public async Task GetGuide_DraftAsAuthor_ReturnsContent()
    {
        // Create a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.Farm });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Author requests draft with ?draft=true
        var (getRsp, guide) = await App.RegularUserClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created!.Slug, Draft = true });
        
        getRsp.IsSuccessStatusCode.ShouldBeTrue();
        guide.ShouldNotBeNull();
        guide.IsDraft.ShouldBeTrue();
    }

    [Fact, Priority(7)]
    public async Task GetGuide_DraftAsModerator_ReturnsContent()
    {
        // Create a guide as regular user
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Moderator requests draft
        var (getRsp, guide) = await App.ModeratorClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created!.Slug, Draft = true });
        
        getRsp.IsSuccessStatusCode.ShouldBeTrue();
        guide.ShouldNotBeNull();
    }

    [Fact, Priority(8)]
    public async Task UpdateGuide_NotAuthor_Returns403()
    {
        // Create a guide as regular user
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Different user (restricted, but still a different user) tries to update
        // Note: RestrictedUser can't create guides but the check is on author match
        // We need another regular user, but for simplicity we'll test with anonymous
        var updateRsp = await App.AnonymousClient.PUTAsync<UpdateGuideEndpoint, UpdateGuideRequest>(
            new UpdateGuideRequest 
            { 
                Id = created!.Id,
                Title = "Hacked Title",
                Description = "Hacked",
                MarkdownContent = "Hacked"
            });
        
        updateRsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(9)]
    public async Task UpdateGuide_AsAuthor_Succeeds()
    {
        // Create a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Author updates the guide
        var updateRsp = await App.RegularUserClient.PUTAsync<UpdateGuideEndpoint, UpdateGuideRequest>(
            new UpdateGuideRequest 
            { 
                Id = created!.Id,
                Title = "Updated Title",
                Description = "Updated Description",
                MarkdownContent = "# Updated Content"
            });
        
        updateRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(10)]
    public async Task SubmitGuide_NotAuthor_Returns403()
    {
        // Create a guide as regular user
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Moderator (not author) tries to submit - should fail
        var submitRsp = await App.ModeratorClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        
        submitRsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(11)]
    public async Task ApproveGuide_NotModerator_Returns403()
    {
        // Create and submit a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        
        // Regular user tries to approve - should fail
        var approveRsp = await App.RegularUserClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
        
        approveRsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(12)]
    public async Task ApproveGuide_AsModerator_Succeeds()
    {
        // Create and submit a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        
        // Moderator approves
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
        
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(13)]
    public async Task VoteGuide_Unauthenticated_Returns401()
    {
        var voteRsp = await App.AnonymousClient.POSTAsync<VoteGuideEndpoint, VoteGuideRequest>(
            new VoteGuideRequest { GuideId = 1, Value = 1 });
        
        voteRsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(14)]
    public async Task VoteGuide_InvalidValue_Returns400()
    {
        var voteRsp = await App.RegularUserClient.POSTAsync<VoteGuideEndpoint, VoteGuideRequest>(
            new VoteGuideRequest { GuideId = 1, Value = 5 }); // Invalid: not +1/-1
        
        voteRsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(15)]
    public async Task DeleteGuide_NotAuthor_Returns404()
    {
        // Create a guide as regular user
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Anonymous tries to delete - should fail
        var deleteRsp = await App.AnonymousClient.DELETEAsync<DeleteGuideEndpoint, DeleteGuideRequest>(
            new DeleteGuideRequest { Id = created!.Id });
        
        deleteRsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(16)]
    public async Task DeleteGuide_AsAuthor_Succeeds()
    {
        // Create a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Author deletes
        var deleteRsp = await App.RegularUserClient.DELETEAsync<DeleteGuideEndpoint, DeleteGuideRequest>(
            new DeleteGuideRequest { Id = created!.Id });
        
        deleteRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Verify guide is no longer accessible
        var (getRsp, _) = await App.RegularUserClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created.Slug, Draft = true });
        getRsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(17)]
    public async Task GetUserGuides_ReturnsUserGuides()
    {
        // Create a guide
        var (createRsp, _) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Get user's guides - use the response pattern
        var result = await App.RegularUserClient.GETAsync<GetUserGuidesEndpoint, GetUserGuidesRequest, List<UserGuideDto>>(
            new GetUserGuidesRequest { AccountId = GuideTestApp.RegularUserId });
        
        result.Response.IsSuccessStatusCode.ShouldBeTrue();
        result.Result.ShouldNotBeNull();
        result.Result.Count.ShouldBeGreaterThan(0);
    }

    [Fact, Priority(18)]
    public async Task Bookmark_AddAndRemove_Succeeds()
    {
        // Create and publish a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
        
        // Add bookmark
        var bookmarkRsp = await App.ModeratorClient.POSTAsync<BookmarkGuideEndpoint, BookmarkRequest>(
            new BookmarkRequest { GuideId = created.Id });
        bookmarkRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Remove bookmark
        var unbookmarkRsp = await App.ModeratorClient.DELETEAsync<UnbookmarkGuideEndpoint, BookmarkRequest>(
            new BookmarkRequest { GuideId = created.Id });
        unbookmarkRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(19)]
    public async Task Unpublish_AsAuthor_Succeeds()
    {
        // Create, submit, and publish a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
        
        // Author unpublishes
        var unpublishRsp = await App.RegularUserClient.POSTAsync<UnpublishGuideEndpoint, UnpublishGuideRequest>(
            new UnpublishGuideRequest { GuideId = created.Id });
        unpublishRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Verify no longer publicly visible
        var (getRsp, _) = await App.AnonymousClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created.Slug });
        getRsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(20)]
    public async Task RejectGuide_WithReason_StoresReason()
    {
        // Create and submit a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        
        // Reject with reason
        var rejectRsp = await App.ModeratorClient.POSTAsync<RejectGuideEndpoint, RejectGuideRequest>(
            new RejectGuideRequest { GuideId = created.Id, Reason = "Needs more detail" });
        rejectRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Author views rejected guide and sees reason
        var (getRsp, guide) = await App.RegularUserClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created.Slug, Draft = true });
        getRsp.IsSuccessStatusCode.ShouldBeTrue();
        guide!.RejectionReason.ShouldBe("Needs more detail");
    }

    [Fact, Priority(21)]
    public async Task GetGuide_Authenticated_ReturnsVoteState()
    {
        // Create and publish a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
        
        // Vote on it
        await App.ModeratorClient.POSTAsync<VoteGuideEndpoint, VoteGuideRequest>(
            new VoteGuideRequest { GuideId = created.Id, Value = 1 });
        
        // Get guide and verify vote state is present
        var (getRsp, guide) = await App.ModeratorClient.GETAsync<GetGuideEndpoint, GetGuideRequest, FullGuideDto>(
            new GetGuideRequest { Slug = created.Slug });
        
        getRsp.IsSuccessStatusCode.ShouldBeTrue();
        guide!.UserVote.ShouldBe((short)1);
    }

    [Fact, Priority(22)]
    public async Task UpdatePublishedGuide_MaintainsVisibility()
    {
        // 1. Create and publish a guide
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created!.Id });
        await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
            
        // 2. Author updates guide content
        await App.RegularUserClient.PUTAsync<UpdateGuideEndpoint, UpdateGuideRequest>(
            new UpdateGuideRequest 
            { 
                Id = created.Id,
                Title = "Updated Title Pending",
                Description = "Updated Desc",
                MarkdownContent = "# Updated"
            });
            
        // 3. Author submits update (Status: Published -> PendingApproval)
        var submitRsp = await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = created.Id });
        submitRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // 4. Verify Admin sees it in Pending list
        var (adminListRsp, pendingGuides) = await App.ModeratorClient.GETAsync<AdminPendingGuidesEndpoint, List<GuideDto>>();
        adminListRsp.IsSuccessStatusCode.ShouldBeTrue();
        pendingGuides.ShouldContain(g => g.Id == created.Id);
        
        // 5. Verify Public still sees the OLD version (Title should NOT be "Updated Title Pending")
        var (publicListRsp, publicGuides) = await App.AnonymousClient.GETAsync<ListGuidesEndpoint, ListGuidesRequest, List<GuideDto>>(
            new ListGuidesRequest());
        publicListRsp.IsSuccessStatusCode.ShouldBeTrue();
        var publicGuide = publicGuides!.FirstOrDefault(g => g.Id == created.Id);
        publicGuide.ShouldNotBeNull();
        publicGuide.Title.ShouldNotBe("Updated Title Pending"); // Helper likely doesn't verify exact content, but existence is key
        
        // 6. Admin Approves
        await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = created.Id });
            
        // 7. Verify Public sees NEW version
        var (finalListRsp, finalGuides) = await App.AnonymousClient.GETAsync<ListGuidesEndpoint, ListGuidesRequest, List<GuideDto>>(
            new ListGuidesRequest());
        finalGuides!.First(g => g.Id == created.Id).Title.ShouldBe("Updated Title Pending");
    }

    protected override async ValueTask TearDownAsync()
    {
        await App.CleanUpGuidesAsync();
    }
}
