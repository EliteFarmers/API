using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;

namespace EliteAPI.Features.Guides.Models;

public class Comment
{
    [Key]
    public int Id { get; set; }

    public int TargetId { get; set; }
    public EliteAPI.Features.Comments.Models.CommentTargetType TargetType { get; set; }

    public required ulong AuthorId { get; set; }
    public EliteAccount Author { get; set; } = null!;

    public int? ParentId { get; set; }
    public Comment? Parent { get; set; }
    public List<Comment> Replies { get; set; } = [];

    [MaxLength(2048)]
    public required string Content { get; set; }

    public bool IsApproved { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    
    // If set, this comment is "lifted" into the main guide view
    [MaxLength(128)]
    public string? LiftedElementId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public ulong? EditedByAdminId { get; set; }
    
    // Cached aggregate score
    public int Score { get; set; } = 0;
    
    public List<CommentVote> Votes { get; set; } = [];
}
