using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Monetization;

public class WeightStyle {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

    /// <summary>
    /// Formatter ID to pass data into for the bot
    /// </summary>
    [MaxLength(64)]
    public string StyleFormatter { get; set; } = "data";
    
    [MaxLength(64)]
	public required string Name { get; set; }
    [MaxLength(64)]
    public string? Collection { get; set; }
    [MaxLength(1024)]
    public string? Description { get; set; }
    
    public List<WeightStyleImage> Images { get; set; } = [];
    
    public List<ProductWeightStyle> ProductWeightStyles { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    
	[Column(TypeName = "jsonb")]
	public WeightStyleData Data { get; set; } = new();
}

public class WeightStyleData
{
    public WeightStyleDecal? Decal { get; set; }
    public WeightStyleElements Elements { get; set; } = new();
}

public class WeightStyleDecal {
    public WeightStylePosition Start { get; set; } = new();
    public WeightStylePosition End { get; set; } = new();
    public string? Fill { get; set; }
    public string? ImageUrl { get; set; } 
    public Dictionary<string, string>? Crops { get; set; }
}

public class WeightStyleElements {
    public WeightStyleBackground Background { get; set; } = new();
    public List<WeightStyleGradient>? Gradients { get; set; }
    public WeightStyleElement? Name { get; set; }
    public WeightStyleElement? Weight { get; set; }
    public WeightStyleElement? Label { get; set; }
    public WeightStyleElement? Head { get; set; }
    public WeightStyleElement? Badge { get; set; }
    public WeightStyleElement? Rank { get; set; }
    public WeightStyleElement? RankWithBadge { get; set; }
}

public class WeightStyleBackground
{
    public WeightStylePosition? Size { get; set; }
    public string? Fill { get; set; }
    public List<WeightStyleBackgroundRect>? Rects { get; set; }
    public string? ImageUrl { get; set; }
    public int? Radius { get; set; }
    public double? Opacity { get; set; }
}

public class WeightStyleBackgroundRect {
    public WeightStylePosition Start { get; set; } = new();
    public WeightStylePosition End { get; set; } = new();
    public string? Fill { get; set; }
    public bool? UseEmbedColor { get; set; }
    public double? Opacity { get; set; }
}

public class WeightStyleGradient
{
    public WeightStyleDirection Direction { get; set; } = new();
    public WeightStyleDirection Bounds { get; set; } = new();
    public List<WeightStyleGradientStop>? Stops { get; set; }
    public double? Opacity { get; set; }
}

public class WeightStyleDirection {
    public WeightStylePosition Start { get; set; } = new();
    public WeightStylePosition End { get; set; } = new();
}

public class WeightStyleGradientStop
{
    public double Position { get; set; }
    public required string Fill { get; set; }
}

public class WeightStyleElement
{
    public string? Font { get; set; }
    public string? Fill { get; set; }
    public int? FontSize { get; set; }
    public WeightStylePosition Position { get; set; } = new();
    public double? MaxWidth { get; set; }
    public double? MaxHeight { get; set; }
    public WeightStyleElementOutline? Outline { get; set; }
    public WeightStyleTextBackground? Background { get; set; }
}

public class WeightStylePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class WeightStyleElementOutline
{
    public int? Width { get; set; }
    public double? Opacity { get; set; }
    public string? Fill { get; set; }
}

public class WeightStyleTextBackground
{
    public string? Fill { get; set; }
    public double? Opacity { get; set; }
    public int? Padding { get; set; }
    public int? Radius { get; set; }
}