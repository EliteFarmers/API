using System.Text.Json.Serialization;

namespace EliteAPI.Features.Common.Models.Dtos;

public class AuthorDto
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Avatar { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uuid { get; set; }
}
