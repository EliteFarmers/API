using EliteAPI.Features.Guides.Services;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Account.Models;
using EliteAPI.Data;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guides.Endpoints;

public class CreateGuideEndpoint(GuideService guideService, DataContext db) : Endpoint<CreateGuideRequest, GuideResponse>
{
    public override void Configure()
    {
        Post("/guides");
        Summary(s => 
        {
            s.Summary = "Create a new guide draft";
            s.Description = "Initializes a new empty guide draft for the user.";
        });
        Description(b => b.Produces<GuideResponse>(201).Produces(401).Produces(403));
    }

    public override async Task HandleAsync(CreateGuideRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Check for RestrictedFromGuides permission
        var account = await db.Accounts.FindAsync(userId.Value);
        if (account != null && (account.Permissions & PermissionFlags.RestrictedFromGuides) != 0)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Check author slot availability (3 + approved guides)
        var canCreate = await guideService.CanCreateGuideAsync(userId.Value);
        if (!canCreate)
        {
            var currentCount = await guideService.GetAuthorGuideCountAsync(userId.Value);
            var maxSlots = await guideService.GetAuthorMaxSlotsAsync(userId.Value);
            ThrowError($"You have reached your guide limit ({currentCount}/{maxSlots}). Get more guides approved to increase your limit.");
            return;
        }
        
        var guide = await guideService.CreateDraftAsync(userId.Value, req.Type);
        
        await Send.OkAsync(new GuideResponse
        {
            Id = guide.Id,
            Slug = guide.Slug!,
            Status = guide.Status.ToString(),
            Title = guide.DraftVersion!.Title
        }, ct);
    }
}

public class CreateGuideRequest
{
    public GuideType Type { get; set; }
}

public class CreateGuideValidator : Validator<CreateGuideRequest>
{
    public CreateGuideValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid guide type.");
    }
}

public class GuideResponse
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
