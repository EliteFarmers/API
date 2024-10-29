using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Models.Entities.Images;

namespace EliteAPI.Models.DTOs.Outgoing;

public class WeightStyleDto 
{
    public int Id { get; set; }
    
    [MaxLength(64)]
    public string? StyleFormatter { get; set; } = "data";
    [MaxLength(64)]
    public string? Name { get; set; }
    [MaxLength(64)]
    public string? Collection { get; set; }
    [MaxLength(1024)]
    public string? Description { get; set; }
    
    public ImageAttachmentDto? Image { get; set; }
    public List<ParentProductDto> Products { get; set; } = [];
}

public class WeightStyleWithDataDto : WeightStyleDto
{
    public WeightStyleDataDto? Data { get; set; }
}

public class WeightStyleLinkedDto
{
    public int Id { get; set; }
    [MaxLength(64)]
    public string? Name { get; set; }
}

public class ImageAttachmentDto
{
    /// <summary>
    /// Image title
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), MaxLength(64)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Image description
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), MaxLength(512)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Image ordering number
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Order { get; set; }
    
    /// <summary>
    /// Full image URL
    /// </summary>
    public string Url { get; set; } = null!;
}

public class UploadImageDto
{
    [MaxLength(64)]
    public string? Title { get; set; }
    [MaxLength(512)]
    public string? Description { get; set; }
    public int? Order { get; set; }
    
    [AllowedFileExtensions]
    public IFormFile Image { get; set; } = null!;
}

public class ParentProductDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
}

public class WeightStyleDataDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleDecalDto? Decal { get; set; }
    
    public WeightStyleElementsDto Elements { get; set; } = new();
}

public class WeightStyleDecalDto {
    public WeightStylePositionDto Start { get; set; } = new();
    
    public WeightStylePositionDto End { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Crops { get; set; }
}

public class WeightStyleElementsDto {
    public WeightStyleBackgroundDto Background { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeightStyleGradientDto>? Gradients { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? Name { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? Weight { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? Label { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? Head { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? Badge { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? Rank { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementDto? RankWithBadge { get; set; }
}

public class WeightStyleBackgroundDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStylePositionDto? Size { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeightStyleBackgroundRectDto>? Rects { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Radius { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Opacity { get; set; }
}

public class WeightStyleBackgroundRectDto 
{
    public WeightStylePositionDto Start { get; set; } = new();
    
    public WeightStylePositionDto End { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UseEmbedColor { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Opacity { get; set; }
}

public class WeightStyleGradientDto
{
    public WeightStyleDirectionDto Direction { get; set; } = new();
    public WeightStyleDirectionDto Bounds { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeightStyleGradientStopDto>? Stops { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Opacity { get; set; }
}

public class WeightStyleDirectionDto {
    public WeightStylePositionDto Start { get; set; } = new();
    public WeightStylePositionDto End { get; set; } = new();
}

public class WeightStyleGradientStopDto
{
    public double Position { get; set; }
    public required string Fill { get; set; }
}

public class WeightStyleElementDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Font { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FontSize { get; set; }
    public WeightStylePositionDto Position { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MaxWidth { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MaxHeight { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleElementOutlineDto? Outline { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleTextBackgroundDto? Background { get; set; }
}

public class WeightStylePositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class WeightStyleElementOutlineDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Width { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Opacity { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
}

public class WeightStyleTextBackgroundDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Opacity { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Padding { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Radius { get; set; }
}