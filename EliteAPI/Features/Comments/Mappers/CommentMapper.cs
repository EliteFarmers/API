using EliteAPI.Features.Comments.Models.Dtos;
using EliteAPI.Features.Common.Models.Dtos;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Models;
using FastEndpoints;

namespace EliteAPI.Features.Comments.Mappers;

[RegisterService<CommentMapper>(LifeTime.Scoped)]
public class CommentMapper
{
    public CommentDto ToDto(Comment comment, ulong? currentUserId, bool isModerator, int? userVote)
    {
        AuthorDto author;
        string content;

        if (comment.IsDeleted)
        {
            content = "[deleted]";
            author = new AuthorDto
            {
                Id = "0",
                Name = "[deleted]",
                Avatar = null
            };
        }
        else
        {
            content = comment.Content;
            author = new AuthorDto
            {
                Id = comment.AuthorId.ToString(),
                Name = comment.Author.GetFormattedIgn(),
                Avatar = comment.Author.HasMinecraftAccount() ? null : comment.Author.Avatar
            };
        }

        string? draftContent = null;
        if ((currentUserId != null && comment.AuthorId == currentUserId.Value) || isModerator)
        {
            draftContent = comment.DraftContent;
        }

        return new CommentDto
        {
            Id = comment.Id,
            Sqid = SqidService.Encode(comment.Id),
            ParentId = comment.ParentId,
            CreatedAt = comment.CreatedAt,
            Score = comment.Score,
            LiftedElementId = comment.LiftedElementId,
            IsPending = !comment.IsApproved,
            IsDeleted = comment.IsDeleted,
            IsEdited = comment.EditedAt != null,
            IsEditedByAdmin = comment.EditedByAdminId != null,
            HasPendingEdit = comment.DraftContent != null,
            EditedAt = comment.EditedAt,
            UserVote = userVote,
            Content = content,
            Author = author,
            DraftContent = draftContent
        };
    }
}
