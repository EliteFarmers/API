using System.Text.Json.Serialization;
using EliteAPI.Features.Common.Models.Dtos;

namespace EliteAPI.Features.Guides.Models.Dtos;

public class GuideDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IconSkyblockId { get; set; }
    
    public required AuthorDto Author { get; set; }
}
