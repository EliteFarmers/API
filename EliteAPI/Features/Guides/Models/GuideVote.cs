using System.ComponentModel.DataAnnotations;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Models;

[PrimaryKey(nameof(GuideId), nameof(UserId))]
public class GuideVote
{
    public int GuideId { get; set; }
    public Guide Guide { get; set; } = null!;

    public ulong UserId { get; set; }
    public EliteAccount User { get; set; } = null!;

    // 1 for Upvote, -1 for Downvote
    public short Value { get; set; }
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
