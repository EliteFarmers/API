using System.Text.Json.Serialization;
using EliteAPI.Features.Common.Models.Dtos;

namespace EliteAPI.Features.Comments.Models.Dtos;

public class CommentDto
{
    public int Id { get; set; }
    public string Sqid { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ParentId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DraftContent { get; set; }
    
    public required AuthorDto Author { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? EditedAt { get; set; }
    
    public int Score { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LiftedElementId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UserVote { get; set; }
    
    public bool IsPending { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsEdited { get; set; }
    public bool IsEditedByAdmin { get; set; }
    public bool HasPendingEdit { get; set; }
}
