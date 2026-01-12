using System.Net;
using EliteAPI.Data;
using EliteAPI.Features.AuditLogs.Endpoints;
using EliteAPI.Features.Comments.Models.Dtos;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Endpoints;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Models.Dtos;
using EliteAPI.Features.Notifications.Endpoints;
using EliteAPI.Features.Notifications.Models;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class NotificationFlowTests(GuideTestApp App) : TestBase
{
    private async Task<int> CreateAndSubmitGuideAsync()
    {
        var (createRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        createRsp.IsSuccessStatusCode.ShouldBeTrue();

        var updateRsp = await App.RegularUserClient.PUTAsync<UpdateGuideEndpoint, UpdateGuideRequest>(
            new UpdateGuideRequest 
            { 
                Id = guide!.Id, 
                Title = "Test Guide for Notifications", 
                Description = "Test Description",
                MarkdownContent = "Test Content" 
            });
        updateRsp.IsSuccessStatusCode.ShouldBeTrue();

        var submitRsp = await App.RegularUserClient.POSTAsync<SubmitGuideForApprovalEndpoint, SubmitGuideRequest>(
            new SubmitGuideRequest { GuideId = guide.Id });
        submitRsp.IsSuccessStatusCode.ShouldBeTrue();

        return guide.Id;
    }

    [Fact, Priority(1)]
    public async Task ApproveGuide_CreatesNotificationForAuthor()
    {
        var guideId = await CreateAndSubmitGuideAsync();

        // Approve the guide as moderator
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = guideId });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Check that a notification was created for the author
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var notification = await db.Notifications
            .Where(n => n.UserId == GuideTestApp.RegularUserId && n.Type == NotificationType.GuideApproved)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        notification.ShouldNotBeNull();
        notification.Title.ShouldContain("approved");
    }

    [Fact, Priority(2)]
    public async Task RejectGuide_CreatesNotificationForAuthor()
    {
        var guideId = await CreateAndSubmitGuideAsync();

        // Reject the guide as moderator
        var rejectRsp = await App.ModeratorClient.POSTAsync<RejectGuideEndpoint, RejectGuideRequest>(
            new RejectGuideRequest { GuideId = guideId, Reason = "Needs more detail" });
        rejectRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Check that a notification was created
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var notification = await db.Notifications
            .Where(n => n.UserId == GuideTestApp.RegularUserId && n.Type == NotificationType.GuideRejected)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        notification.ShouldNotBeNull();
        notification.Message!.ShouldContain("Needs more detail");
    }

    [Fact, Priority(3)]
    public async Task ApproveGuide_CreatesAuditLog()
    {
        var guideId = await CreateAndSubmitGuideAsync();

        // Approve the guide
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = guideId });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Check audit log directly in database
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var auditLog = await db.AdminAuditLogs
            .Where(l => l.TargetId == SqidService.Encode(guideId) && l.Action == "guide_approved")
            .FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        auditLog.ShouldNotBeNull();
        auditLog.AdminUserId.ShouldBe(GuideTestApp.ModeratorUserId);
    }

    [Fact, Priority(4)]
    public async Task ApproveComment_CreatesNotificationForAuthor()
    {
        // Create guide
        var (guideRsp, guide) = await App.RegularUserClient.POSTAsync<CreateGuideEndpoint, CreateGuideRequest, GuideDto>(
            new CreateGuideRequest { Type = GuideType.General });
        guideRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Create comment
        var (commentRsp, comment) = await App.RegularUserClient.POSTAsync<CreateCommentEndpoint, CreateCommentRequest, CommentDto>(
            new CreateCommentRequest { GuideId = guide!.Id, Content = "Test comment" });
        commentRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Approve comment
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveCommentEndpoint, ApproveCommentRequest>(
            new ApproveCommentRequest { CommentId = comment!.Id });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Check notification
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var notification = await db.Notifications
            .Where(n => n.UserId == GuideTestApp.RegularUserId && n.Type == NotificationType.CommentApproved)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        notification.ShouldNotBeNull();
    }

    [Fact, Priority(5)]
    public async Task MarkNotificationAsRead_UpdatesIsReadFlag()
    {
        // First create a notification by approving a guide
        var guideId = await CreateAndSubmitGuideAsync();
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = guideId });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Get the notification
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var notification = await db.Notifications
            .Where(n => n.UserId == GuideTestApp.RegularUserId)
            .OrderByDescending(n => n.CreatedAt)
            .FirstAsync(cancellationToken: TestContext.Current.CancellationToken);

        notification.IsRead.ShouldBeFalse();

        // Mark as read via endpoint
        var markReadRsp = await App.RegularUserClient.PostAsync($"/notifications/{notification.Id}/read", null, TestContext.Current.CancellationToken);
        markReadRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Verify in DB
        await db.Entry(notification).ReloadAsync(TestContext.Current.CancellationToken);
        notification.IsRead.ShouldBeTrue();
    }

    [Fact, Priority(6)]
    public async Task GetNotifications_ReturnsNotificationsForUser()
    {
        // Create a notification
        var guideId = await CreateAndSubmitGuideAsync();
        var approveRsp = await App.ModeratorClient.POSTAsync<ApproveGuideEndpoint, ApproveGuideRequest>(
            new ApproveGuideRequest { GuideId = guideId });
        approveRsp.IsSuccessStatusCode.ShouldBeTrue();

        // Get notifications
        var (rsp, res) = await App.RegularUserClient.GETAsync<GetNotificationsEndpoint, GetNotificationsRequest, GetNotificationsResponse>(
            new GetNotificationsRequest());

        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Notifications.ShouldNotBeEmpty();
        res.UnreadCount.ShouldBeGreaterThan(0);
    }

    protected override async ValueTask TearDownAsync()
    {
        await App.CleanUpGuidesAsync();
        
        // Clean up notifications for test users
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var testUserIds = new[] { GuideTestApp.RegularUserId, GuideTestApp.ModeratorUserId, GuideTestApp.AdminUserId };
        var notifications = await db.Notifications.Where(n => testUserIds.Contains(n.UserId)).ToListAsync();
        db.Notifications.RemoveRange(notifications);
        var auditLogs = await db.AdminAuditLogs.Where(l => testUserIds.Contains(l.AdminUserId)).ToListAsync();
        db.AdminAuditLogs.RemoveRange(auditLogs);
        await db.SaveChangesAsync();
    }
}
