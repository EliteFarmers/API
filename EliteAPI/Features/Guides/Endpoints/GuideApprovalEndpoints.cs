using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

/// <summary>
/// Submit a guide for approval (author only)
/// </summary>
public class SubmitGuideForApprovalEndpoint(GuideService guideService, UserManager userManager) : Endpoint<SubmitGuideRequest>
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

        // Only author can submit
        if (guide.AuthorId != user.AccountId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Must be in Draft status
        if (guide.Status != Models.GuideStatus.Draft && guide.Status != Models.GuideStatus.Rejected)
        {
            ThrowError("Guide must be in Draft or Rejected status to submit for approval.");
            return;
        }

        await guideService.SubmitForApprovalAsync(req.GuideId);
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
public class ApproveGuideEndpoint(GuideService guideService, UserManager userManager) : Endpoint<ApproveGuideRequest>
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

        await guideService.PublishAsync(req.GuideId, user?.AccountId ?? 0);
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
public class RejectGuideEndpoint(GuideService guideService) : Endpoint<RejectGuideRequest>
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
        var guide = await guideService.GetByIdAsync(req.GuideId);
        if (guide == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var success = await guideService.RejectGuideAsync(req.GuideId, 0, req.Reason);
        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
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
