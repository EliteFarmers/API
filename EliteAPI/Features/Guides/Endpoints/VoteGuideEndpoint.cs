using EliteAPI.Features.Guides.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Features.Guides.Endpoints;

/// <summary>
/// Vote on a guide (+1/-1)
/// </summary>
public class VoteGuideEndpoint(GuideService guideService, UserManager userManager) : Endpoint<VoteGuideRequest>
{
    public override void Configure()
    {
        Post("/guides/{GuideId}/vote");
        Summary(s =>
        {
            s.Summary = "Vote on a guide";
            s.Description = "Cast an upvote (+1) or downvote (-1) on a guide.";
        });
    }

    public override async Task HandleAsync(VoteGuideRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            await guideService.VoteGuideAsync(req.GuideId, user.AccountId.Value, req.Value);
            await Send.NoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await Send.NotFoundAsync(ct);
        }
        catch (ArgumentException)
        {
            ThrowError("Invalid vote value");
        }
    }
}

public class VoteGuideRequest
{
    public int GuideId { get; set; }
    public short Value { get; set; }
}

public class VoteGuideValidator : Validator<VoteGuideRequest>
{
    public VoteGuideValidator()
    {
        RuleFor(x => x.Value)
            .Must(v => v == 1 || v == -1)
            .WithMessage("Vote value must be either 1 (upvote) or -1 (downvote).");
    }
}
