using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using EliteAPI.Features.Images.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public enum CosmeticType {
    WeightStyle = 0,
}

[Table("Cosmetics")]
[Index(nameof(Type))]
public class WeightStyle {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
    
    public CosmeticType Type { get; set; } = CosmeticType.WeightStyle;

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
    
    public Image? Image { get; set; }
    public List<Image> Images { get; set; } = [];
    
    public List<ProductWeightStyle> ProductWeightStyles { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    
	[Column(TypeName = "jsonb")]
	public WeightStyleData Data { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public LeaderboardStyleData? Leaderboard { get; set; }
    
    [Column(TypeName = "jsonb")]
    public NameStyleData? NameStyle { get; set; }

    public bool HasLeaderboardStyle() => Leaderboard is not null &&
         (Leaderboard.Background is not null || Leaderboard.Overlay is not null ||
          Leaderboard.Name is not null ||
          Leaderboard.Score is not null || Leaderboard.Rank is not null ||
          Leaderboard.Subtitle is not null);
}

public class CosmeticEntityConfiguration : IEntityTypeConfiguration<WeightStyle>
{
    public void Configure(EntityTypeBuilder<WeightStyle> builder)
    {
        builder.Navigation(p => p.Image).AutoInclude();
        builder.Navigation(p => p.Images).AutoInclude();
        
        builder
            .HasMany(e => e.Images)
            .WithMany()
            .UsingEntity<CosmeticImage>(
                j => j.HasOne(ci => ci.Image).WithMany().HasForeignKey(ci => ci.ImageId),
                j => j.HasOne(ci => ci.Cosmetic).WithMany().HasForeignKey(ci => ci.CosmeticId)
            );
    }
}


public class LeaderboardStyleData
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderboardStyleLayer? Background { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderboardStyleLayer? Overlay { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? GradientOpacity { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GradientColor { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Font { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderboardStyleText? Name { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderboardStyleText? Score { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderboardStyleText? Rank { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderboardStyleText? Subtitle { get; set; }
}

public class LeaderboardStyleLayer
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageOpacity { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FillColor { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FillOpacity { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BorderColor { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? BorderOpacity { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Align { get; set; }
}

public class LeaderboardStyleText
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ShadowColor { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ShadowOpacity { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FontWeight { get; set; }
}

public class NameStyleData
{
    public List<NameStyleEmojiOption> Emojis { get; set; } = [];
}

public class NameStyleEmojiOption
{
    public string? Name { get; set; }
    public string? Url { get; set; }
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