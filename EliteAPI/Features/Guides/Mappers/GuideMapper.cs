using EliteAPI.Features.Common.Models.Dtos;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Models.Dtos;
using FastEndpoints;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Guides.Mappers;

[RegisterService<GuideMapper>(LifeTime.Scoped)]
[Mapper]
public partial class GuideMapper
{
    public partial GuideDto GuideToDto(Guide guide);

    public partial UserGuideDto GuideToUserGuideDto(Guide guide);
    
    [UserMapping(Default = true)]
    public GuideDto ToDto(Guide guide)
    {
        return new GuideDto
        {
            Id = guide.Id,
            Slug = SqidService.Encode(guide.Id),
            Status = guide.Status.ToString(),
            IconSkyblockId = guide.IconSkyblockId,
            Title = guide.ActiveVersion?.Title ?? "Untitled",
            Views = guide.ViewCount,
            Score = guide.Score,
            CreatedAt = guide.CreatedAt,
            Description = guide.ActiveVersion?.Description ?? "No description.",
            Tags = guide.Tags.Select(t => t.Tag.Name).ToList(),
            Author = new AuthorDto
            {
                Id = guide.AuthorId.ToString(),
                Name = guide.Author.GetFormattedIgn(),
                Avatar = guide.Author.HasMinecraftAccount() ? null : guide.Author.Avatar,
                Uuid = guide.Author.MinecraftAccounts.FirstOrDefault(a => a.Selected)?.Id
            },
        };
    }
    
    [UserMapping(Default = true)]
    public UserGuideDto ToUserGuideDto(Guide guide)
    {
        var version = guide.ActiveVersion ?? guide.DraftVersion;
        
        return new UserGuideDto
        {
            Id = guide.Id,
            Slug = SqidService.Encode(guide.Id),
            Title = version?.Title ?? "Untitled",
            Description = version?.Description ?? "",
            Type = guide.Type.ToString(),
            Status = guide.Status.ToString(),
            Score = guide.Score,
            ViewCount = guide.ViewCount,
            CreatedAt = guide.CreatedAt,
            UpdatedAt = guide.UpdatedAt
        };
    }

    public FullGuideDto ToFullGuideDto(Guide guide, GuideVersion version, short? userVote, bool? isBookmarked)
    {
        return new FullGuideDto
        {
            Id = guide.Id,
            Slug = SqidService.Encode(guide.Id),
            Title = version.Title,
            Description = version.Description,
            IconSkyblockId = guide.IconSkyblockId,
            Content = version.MarkdownContent,
            Author = new AuthorDto
            {
                Id = guide.AuthorId.ToString(),
                Name = guide.Author.GetFormattedIgn(),
                Avatar = guide.Author.HasMinecraftAccount() ? null : guide.Author.Avatar,
                Uuid = guide.Author.MinecraftAccounts.FirstOrDefault(a => a.Selected)?.Id
            },
            CreatedAt = guide.CreatedAt,
            Score = guide.Score,
            Status = guide.Status.ToString(),
            ViewCount = guide.ViewCount,
            Tags = guide.Tags.Select(t => t.Tag.Name).ToList(),
            IsDraft = version.Id == guide.DraftVersionId, 
            UserVote = userVote,
            IsBookmarked = isBookmarked,
            RejectionReason = guide.Status == GuideStatus.Rejected ? guide.RejectionReason : null
        };
    }
}
