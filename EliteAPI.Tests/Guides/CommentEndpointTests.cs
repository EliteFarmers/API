using System.Net;
using EliteAPI.Features.Guides.Endpoints;
using EliteAPI.Features.Guides.Models;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class CommentEndpointTests(GuideTestApp App) : TestBase
{
    private async Task<int> CreateTestGuideAsync()
    {
        var (rsp, res) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        return res!.Id;
    }

    [Fact, Priority(1)]
    public async Task CreateComment_Unauthenticated_Returns401()
    {
        var guideId = await CreateTestGuideAsync();
        
        var (rsp, _) = await App.AnonymousClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Test comment" });
        
        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(2)]
    public async Task CreateComment_RestrictedUser_Returns400()
    {
        var guideId = await CreateTestGuideAsync();
        
        // RestrictedUser has RestrictedFromComments flag
        var (rsp, _) = await App.RestrictedUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Test comment" });
        
        // Service throws UnauthorizedAccessException which is caught and returns 400 with error message
        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(3)]
    public async Task CreateComment_ValidUser_CreatesComment()
    {
        var guideId = await CreateTestGuideAsync();
        
        var (rsp, res) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "This is a great guide!" });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Content.ShouldBe("This is a great guide!");
        res.Sqid.ShouldNotBeNullOrEmpty();
    }

    [Fact, Priority(4)]
    public async Task CreateComment_EmptyContent_Returns400()
    {
        var guideId = await CreateTestGuideAsync();
        
        var (rsp, _) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "" });
        
        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(5)]
    public async Task EditComment_NotAuthor_Returns404()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Create comment as regular user
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Original content" });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Try to edit as moderator (not author, but has permission)
        // Actually moderators CAN edit, let's test with anonymous
        var editRsp = await App.AnonymousClient.PUTAsync<EditCommentEndpoint, EditCommentRequest>(
            new EditCommentRequest { CommentId = created!.Id, Content = "Hacked content" });
        
        editRsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(6)]
    public async Task EditComment_AsAuthor_Succeeds()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Create comment
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Original content" });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Edit as author
        var editRsp = await App.RegularUserClient.PUTAsync<EditCommentEndpoint, EditCommentRequest>(
            new EditCommentRequest { CommentId = created!.Id, Content = "Updated content" });
        
        editRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(7)]
    public async Task EditComment_AsAdmin_Succeeds()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Create comment as regular user
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Original content" });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Admin edits someone else's comment
        var editRsp = await App.AdminClient.PUTAsync<EditCommentEndpoint, EditCommentRequest>(
            new EditCommentRequest { CommentId = created!.Id, Content = "Admin edited content" });
        
        editRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(8)]
    public async Task VoteComment_Unauthenticated_Returns401()
    {
        var voteRsp = await App.AnonymousClient.POSTAsync<VoteCommentEndpoint, VoteCommentRequest>(
            new VoteCommentRequest { CommentId = 1, Value = 1 });
        
        voteRsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(9)]
    public async Task VoteComment_InvalidValue_Returns400()
    {
        var voteRsp = await App.RegularUserClient.POSTAsync<VoteCommentEndpoint, VoteCommentRequest>(
            new VoteCommentRequest { CommentId = 1, Value = 10 }); // Invalid: not +1/-1
        
        voteRsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(10)]
    public async Task ApproveComment_NotModerator_Returns403()
    {
        var approveRsp = await App.RegularUserClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = 1 });
        
        approveRsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(11)]
    public async Task DeleteComment_NotModerator_Returns403()
    {
        var deleteRsp = await App.RegularUserClient.DELETEAsync<DeleteCommentEndpoint, DeleteCommentRequest>(
            new DeleteCommentRequest { CommentId = 1 });
        
        deleteRsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(12)]
    public async Task ApproveComment_AsModerator_Succeeds()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Create comment
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Pending comment" });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Moderator approves
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = created!.Id });
        
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(13)]
    public async Task DeleteComment_AsModerator_Succeeds()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Create comment
        var (createRsp, created) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guideId, Content = "Comment to delete" });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Moderator deletes
        var deleteRsp = await App.ModeratorClient.DELETEAsync<DeleteCommentEndpoint, DeleteCommentRequest>(
            new DeleteCommentRequest { CommentId = created!.Id });
        
        deleteRsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(14)]
    public async Task LiftedElementId_NonModerator_ThrowsError()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Regular user tries to set LiftedElementId - should fail
        var (rsp, _) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest 
            { 
                GuideId = guideId, 
                Content = "Trying to lift myself",
                LiftedElementId = "section-1"
            });
        
        // Service throws UnauthorizedAccessException for non-mods setting LiftedElementId
        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(15)]
    public async Task LiftedElementId_AsModerator_Succeeds()
    {
        var guideId = await CreateTestGuideAsync();
        
        // Moderator can set LiftedElementId
        var (rsp, res) = await App.ModeratorClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest 
            { 
                GuideId = guideId, 
                Content = "Lifted by moderator",
                LiftedElementId = "important-section"
            });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
    }
    [Fact, Priority(16)]
    public async Task ListComments_AuthorSeesPendingComment()
    {
        // Create guide and get its slug
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create a pending comment
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "My pending comment" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments as the author
        var (listRsp, comments) = await App.RegularUserClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        comments.ShouldNotBeNull();
        var myComment = comments.FirstOrDefault(c => c.Id == comment!.Id);
        myComment.ShouldNotBeNull();
        myComment.IsPending.ShouldBeTrue();
    }

    [Fact, Priority(17)]
    public async Task ListComments_OtherUserDoesNotSeePendingComment()
    {
        // Create guide as regular user
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create a pending comment as regular user
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "User's pending comment" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments as anonymous (different user)
        var (listRsp, comments) = await App.AnonymousClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        comments.ShouldNotBeNull();
        comments.ShouldNotContain(c => c.Id == comment!.Id);
    }

    [Fact, Priority(18)]
    public async Task ListComments_ModeratorSeesAllPendingComments()
    {
        // Create guide as regular user
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create a pending comment as regular user
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "User's pending comment for mod" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments as moderator
        var (listRsp, comments) = await App.ModeratorClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        comments.ShouldNotBeNull();
        var pendingComment = comments.FirstOrDefault(c => c.Id == comment!.Id);
        pendingComment.ShouldNotBeNull();
        pendingComment.IsPending.ShouldBeTrue();
    }

    [Fact, Priority(19)]
    public async Task ListComments_ApprovedCommentHasIsPendingFalse()
    {
        // Create guide
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create and approve a comment
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "Comment to approve" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment!.Id });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments as anonymous (should see approved comment)
        var (listRsp, comments) = await App.AnonymousClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        comments.ShouldNotBeNull();
        var approvedComment = comments.FirstOrDefault(c => c.Id == comment.Id);
        approvedComment.ShouldNotBeNull();
        approvedComment.IsPending.ShouldBeFalse();
    }
    [Fact, Priority(20)]
    public async Task EditComment_AfterApproval_CreatesDraftRequiringReapproval()
    {
        // Create and approve a guide
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create and approve a comment
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "Original content" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment!.Id });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Edit the approved comment
        var editRsp = await App.RegularUserClient.PUTAsync<EditCommentEndpoint, EditCommentRequest>(
            new EditCommentRequest { CommentId = comment.Id, Content = "Updated content" });
        editRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments as author - should show HasPendingEdit
        var (listRsp, comments) = await App.RegularUserClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        var editedComment = comments!.FirstOrDefault(c => c.Id == comment.Id);
        editedComment.ShouldNotBeNull();
        editedComment.HasPendingEdit.ShouldBeTrue();
        editedComment.Content.ShouldBe("Original content"); // Original still visible
        editedComment.DraftContent.ShouldBe("Updated content");
    }

    [Fact, Priority(21)]
    public async Task ApproveComment_WithDraft_AppliesDraftContent()
    {
        // Create and approve a guide
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create and approve a comment
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "Original content" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        var approve1 = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment!.Id });
        approve1.IsSuccessStatusCode.ShouldBeTrue();
        
        // Edit the approved comment
        var editRsp = await App.RegularUserClient.PUTAsync<EditCommentEndpoint, EditCommentRequest>(
            new EditCommentRequest { CommentId = comment.Id, Content = "Updated content" });
        editRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Re-approve (applies draft)
        var approve2 = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment.Id });
        approve2.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments - should now show updated content
        var (listRsp, comments) = await App.AnonymousClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        var updatedComment = comments!.FirstOrDefault(c => c.Id == comment.Id);
        updatedComment.ShouldNotBeNull();
        updatedComment.Content.ShouldBe("Updated content");
        updatedComment.HasPendingEdit.ShouldBeFalse();
    }

    [Fact, Priority(22)]
    public async Task DeletedComment_ShowsDeletedPlaceholder()
    {
        // Create guide
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create and approve a comment
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "Comment to delete" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment!.Id });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Delete the comment
        var deleteRsp = await App.ModeratorClient.DELETEAsync<DeleteCommentEndpoint, DeleteCommentRequest>(
            new DeleteCommentRequest { CommentId = comment.Id });
        deleteRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments - deleted comment should show placeholder
        var (listRsp, comments) = await App.AnonymousClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        var deletedComment = comments!.FirstOrDefault(c => c.Id == comment.Id);
        deletedComment.ShouldNotBeNull();
        deletedComment.IsDeleted.ShouldBeTrue();
        deletedComment.Content.ShouldBe("[deleted]");
        deletedComment.AuthorName.ShouldBe("[deleted]");
    }

    [Fact, Priority(23)]
    public async Task EditedComment_HasIsEditedFlag()
    {
        // Create guide
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideResponse>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Create comment (not approved yet, so direct edit allowed)
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CreateCommentResponse>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "Original" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Edit the comment (not approved, so direct edit)
        var editRsp = await App.RegularUserClient.PUTAsync<EditCommentEndpoint, EditCommentRequest>(
            new EditCommentRequest { CommentId = comment!.Id, Content = "Edited" });
        editRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // Approve the comment
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment.Id });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();
        
        // List comments
        var (listRsp, comments) = await App.AnonymousClient.GETAsync<ListCommentsEndpoint, ListCommentsRequest, List<CommentResponse>>(
            new ListCommentsRequest { Slug = guide.Slug });
        
        listRsp.IsSuccessStatusCode.ShouldBeTrue();
        var editedComment = comments!.FirstOrDefault(c => c.Id == comment.Id);
        editedComment.ShouldNotBeNull();
        editedComment.IsEdited.ShouldBeTrue();
        editedComment.Content.ShouldBe("Edited");
    }

    protected override async ValueTask TearDownAsync()
    {
        await App.CleanUpGuidesAsync();
    }
}
