using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Guides.Models;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Endpoints;

/// <summary>
/// List all tags (public).
/// </summary>
public class ListTagsEndpoint(DataContext db) : EndpointWithoutRequest<List<TagResponse>>
{
    public override void Configure()
    {
        Get("/tags");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all tags";
            s.Description = "Returns all available guide tags.";
        });
    }
    public override async Task HandleAsync(CancellationToken ct)
    {
        var tags = await db.GuideTags.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync(ct);
        await Send.OkAsync(tags.Select(t => new TagResponse
        {
            Id = t.Id,
            Name = t.Name,
            Category = t.Category,
            HexColor = t.HexColor
        }).ToList(), ct);
    }
}

/// <summary>
/// Create a new tag (admin only).
/// </summary>
public class CreateTagEndpoint(DataContext db) : Endpoint<CreateTagRequest, TagResponse>
{
    public override void Configure()
    {
        Post("/admin/tags");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Create a tag";
            s.Description = "Create a new guide tag. Moderator only.";
        });
    }

    public override async Task HandleAsync(CreateTagRequest req, CancellationToken ct)
    {
        var tag = new GuideTag
        {
            Name = req.Name,
            Category = req.Category,
            HexColor = req.HexColor ?? "#FFFFFF"
        };

        db.GuideTags.Add(tag);
        await db.SaveChangesAsync(ct);

        await Send.CreatedAtAsync<ListTagsEndpoint>(null, new TagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Category = tag.Category,
            HexColor = tag.HexColor
        }, cancellation: ct);
    }
}

/// <summary>
/// Update a tag (admin only).
/// </summary>
public class UpdateTagEndpoint(DataContext db) : Endpoint<UpdateTagRequest, TagResponse>
{
    public override void Configure()
    {
        Put("/admin/tags/{id}");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Update a tag";
            s.Description = "Update an existing guide tag. Moderator only.";
        });
    }

    public override async Task HandleAsync(UpdateTagRequest req, CancellationToken ct)
    {
        var tag = await db.GuideTags.FirstOrDefaultAsync(g => g.Id == req.Id, ct);
        if (tag == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrEmpty(req.Name)) tag.Name = req.Name;
        if (!string.IsNullOrEmpty(req.Category)) tag.Category = req.Category;
        if (!string.IsNullOrEmpty(req.HexColor)) tag.HexColor = req.HexColor;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new TagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Category = tag.Category,
            HexColor = tag.HexColor
        }, ct);
    }
}

/// <summary>
/// Delete a tag (admin only).
/// </summary>
public class DeleteTagEndpoint(DataContext db) : Endpoint<DeleteTagRequest>
{
    public override void Configure()
    {
        Delete("/admin/tags/{id}");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Delete a tag";
            s.Description = "Delete a guide tag. Moderator only.";
        });
        Description(b => b.Produces(204).Produces(404));
    }

    public override async Task HandleAsync(DeleteTagRequest req, CancellationToken ct)
    {
        var tag = await db.GuideTags.FirstOrDefaultAsync(g => g.Id == req.Id, ct);
        if (tag == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        db.GuideTags.Remove(tag);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}

// Request/Response DTOs

public class TagResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string HexColor { get; set; } = "";
}

public class CreateTagRequest
{
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? HexColor { get; set; }
}

public class CreateTagValidator : Validator<CreateTagRequest>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(32);
        RuleFor(x => x.HexColor).MaximumLength(7).When(x => x.HexColor != null);
    }
}

public class UpdateTagRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? HexColor { get; set; }
}

public class DeleteTagRequest
{
    public int Id { get; set; }
}
