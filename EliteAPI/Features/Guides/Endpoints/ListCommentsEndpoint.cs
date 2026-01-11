using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class ListCommentsEndpoint(CommentService commentService, GuideService guideService) : Endpoint<ListCommentsRequest, List<CommentResponse>>
{
    public override void Configure()
    {
        Get("/guides/{Slug}/comments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListCommentsRequest req, CancellationToken ct)
    {
        var guideId = guideService.GetIdFromSlug(req.Slug);
        if (guideId == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetDiscordId();
        var isModerator = User.IsInRole(ApiUserPolicies.Moderator) || User.IsInRole(ApiUserPolicies.Admin);
        
        var comments = await commentService.GetCommentsAsync(
            guideId.Value, 
            EliteAPI.Features.Comments.Models.CommentTargetType.Guide,
            userId,
            isModerator);

        Dictionary<int, short> userVotes = [];
        
        if (userId != null && comments.Any())
        {
            userVotes = await commentService.GetUserVotesForCommentsAsync(comments.Select(c => c.Id), userId.Value);
        }

        var response = comments.Select(c => new CommentResponse
        {
            Id = c.Id,
            Sqid = SqidService.Encode(c.Id),
            ParentId = c.ParentId,
            Content = c.IsDeleted ? "[deleted]" : c.Content,
            AuthorId = c.IsDeleted ? "0" : c.AuthorId.ToString(),
            AuthorName = c.IsDeleted ? "[deleted]" : c.Author.GetFormattedIgn(),
            AuthorAvatar = c.IsDeleted ? null : (c.Author.HasMinecraftAccount() ? null : c.Author.Avatar),
            CreatedAt = c.CreatedAt,
            Score = c.Score,
            LiftedElementId = c.LiftedElementId,
            UserVote = userVotes.TryGetValue(c.Id, out var vote) ? vote : null,
            IsPending = !c.IsApproved,
            IsDeleted = c.IsDeleted,
            IsEdited = c.EditedAt != null,
            IsEditedByAdmin = c.EditedByAdminId != null,
            HasPendingEdit = c.DraftContent != null,
            EditedAt = c.EditedAt,
            // Return draft content only to author or moderators
            DraftContent = ((userId != null && c.AuthorId == userId.Value) || isModerator) ? c.DraftContent : null
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class ListCommentsRequest
{
    public string Slug { get; set; } = string.Empty;
}

public class CommentResponse
{
    public int Id { get; set; }
    public string Sqid { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? DraftContent { get; set; }
    public required string AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public int Score { get; set; }
    public string? LiftedElementId { get; set; }
    public int? UserVote { get; set; }
    public bool IsPending { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsEdited { get; set; }
    public bool IsEditedByAdmin { get; set; }
    public bool HasPendingEdit { get; set; }
}


