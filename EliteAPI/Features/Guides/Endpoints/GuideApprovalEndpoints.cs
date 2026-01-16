using EliteAPI.Features.AuditLogs.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Notifications.Models;
using EliteAPI.Features.Notifications.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

/// <summary>
/// Submit a guide for approval
/// </summary>
public class SubmitGuideForApprovalEndpoint(
    GuideService guideService, 
    UserManager userManager,
    AuditLogService auditLogService,
    NotificationService notificationService) : Endpoint<SubmitGuideRequest>
{
    public override void Configure()
    {
        Post("/guides/{GuideId}/submit");
        
        Options(x => x.Accepts<SubmitGuideRequest>());
        
        Summary(s =>
        {
            s.Summary = "Submit guide for approval";
            s.Description = "Submit a draft guide for admin review.";
        });
    }

    public override async Task HandleAsync(SubmitGuideRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var guide = await guideService.GetByIdAsync(req.GuideId);
        if (guide == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Only author or moderator can submit
        var isModerator = User.IsModeratorOrHigher();
        if (guide.AuthorId != user.AccountId.Value && !isModerator)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Must be in Draft, Rejected, or Published status (for updates)
        if (guide.Status != Models.GuideStatus.Draft && 
            guide.Status != Models.GuideStatus.Rejected && 
            guide.Status != Models.GuideStatus.Published)
        {
            ThrowError("Guide must be in Draft, Rejected, or Published status to submit for approval.");
            return;
        }

        await guideService.SubmitForApprovalAsync(req.GuideId);
        
        var guideSlug = guideService.GetSlug(guide.Id);

        await auditLogService.LogAsync(
            user.AccountId!.Value,
            "guide_submitted",
            "Guide",
            guideSlug,
            "Submitted guide for approval");
            
        await notificationService.CreateAsync(
            user.AccountId!.Value,
            NotificationType.GuideSubmitted,
            "Guide Submitted",
            $"**{guide.DraftVersion?.Title}** has been submitted for approval!",
            $"/guides/{guideSlug}?draft=true");
            
        await Send.NoContentAsync(ct);
    }
}

public class SubmitGuideRequest
{
    public int GuideId { get; set; }
}

/// <summary>
/// Approve and publish a guide (admin only)
/// </summary>
public class ApproveGuideEndpoint(
    GuideService guideService, 
    UserManager userManager,
    NotificationService notificationService,
    AuditLogService auditLogService) : Endpoint<ApproveGuideRequest>
{
    public override void Configure()
    {
        Post("/admin/guides/{GuideId}/approve");
        Options(x => x.Accepts<ApproveGuideRequest>());
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Approve and publish a guide";
            s.Description = "Approve a pending guide and publish it.";
        });
    }

    public override async Task HandleAsync(ApproveGuideRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        
        var guide = await guideService.GetByIdAsync(req.GuideId);
        if (guide == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (guide.Status != Models.GuideStatus.PendingApproval)
        {
            ThrowError("Guide must be pending approval.");
            return;
        }

        var isNewGuide = guide.ActiveVersionId == null;
        await guideService.PublishAsync(req.GuideId, user?.AccountId ?? 0);

        var notificationType = isNewGuide ? NotificationType.GuideApproved : NotificationType.GuideEditApproved;
        var title = isNewGuide 
            ? "Your guide has been approved!" 
            : "Your guide edit has been approved!";
        var guideTitle = guide.DraftVersion?.Title ?? guide.ActiveVersion?.Title ?? "Guide";

        await notificationService.CreateAsync(
            guide.AuthorId,
            notificationType,
            title,
            $"**{guideTitle}** is now published and visible to everyone.",
            $"/guides/{guideService.GetSlug(guide.Id)}");

        await auditLogService.LogAsync(
            user?.AccountId ?? 0,
            isNewGuide ? "guide_approved" : "guide_edit_approved",
            "Guide",
            SqidService.Encode(guide.Id),
            $"Approved guide: {guideTitle}");

        await Send.NoContentAsync(ct);
    }
}

public class ApproveGuideRequest
{
    public int GuideId { get; set; }
}

/// <summary>
/// Reject a guide (admin only)
/// </summary>
public class RejectGuideEndpoint(
    GuideService guideService,
    UserManager userManager,
    NotificationService notificationService,
    AuditLogService auditLogService) : Endpoint<RejectGuideRequest>
{
    public override void Configure()
    {
        Post("/admin/guides/{GuideId}/reject");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Reject a guide";
            s.Description = "Reject a pending guide submission with an optional reason.";
        });
    }

    public override async Task HandleAsync(RejectGuideRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        
        var guide = await guideService.GetByIdAsync(req.GuideId);
        if (guide == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var success = await guideService.RejectGuideAsync(req.GuideId, user?.AccountId ?? 0, req.Reason);
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var guideTitle = guide.DraftVersion?.Title ?? guide.ActiveVersion?.Title ?? "Guide";
        var message = string.IsNullOrEmpty(req.Reason)
            ? $"**{guideTitle}** was not approved."
            : $"**{guideTitle}** was not approved.\n\n**Reason:** {req.Reason}";

        await notificationService.CreateAsync(
            guide.AuthorId,
            NotificationType.GuideRejected,
            "Your guide was not approved",
            message,
            $"/guides/{guideService.GetSlug(guide.Id)}?draft=true");

        await auditLogService.LogAsync(
            user?.AccountId ?? 0,
            "guide_rejected",
            "Guide",
            guideService.GetSlug(guide.Id),
            $"Rejected guide: {guideTitle}" + (string.IsNullOrEmpty(req.Reason) ? "" : $" - Reason: {req.Reason}"));
        
        await Send.NoContentAsync(ct);
    }
}

public class RejectGuideRequest
{
    public int GuideId { get; set; }
    
    /// <summary>
    /// Optional reason for rejection to provide feedback to the author.
    /// </summary>
    public string? Reason { get; set; }
}

