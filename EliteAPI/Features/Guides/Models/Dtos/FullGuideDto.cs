using System.Text.Json.Serialization;
using EliteAPI.Features.Common.Models.Dtos;

namespace EliteAPI.Features.Guides.Models.Dtos;

public class FullGuideDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IconSkyblockId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    public required AuthorDto Author { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public int Score { get; set; }
    public int ViewCount { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool IsDraft { get; set; }
    public string Status { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public short? UserVote { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsBookmarked { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RejectionReason { get; set; }
}
