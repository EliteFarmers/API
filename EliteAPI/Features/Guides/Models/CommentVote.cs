using System.ComponentModel.DataAnnotations;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guides.Models;

[PrimaryKey(nameof(CommentId), nameof(UserId))]
public class CommentVote
{
    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    public ulong UserId { get; set; }
    public EliteAccount User { get; set; } = null!;

    // 1 for Upvote, -1 for Downvote
    public short Value { get; set; }
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
