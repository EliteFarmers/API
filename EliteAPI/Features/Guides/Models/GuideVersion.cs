using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Features.Guides.Models;

public class GuideVersion
{
    [Key]
    public int Id { get; set; }

    public int GuideId { get; set; }
    public Guide Guide { get; set; } = null!;

    [MaxLength(128)]
    public required string Title { get; set; }

    [MaxLength(512)]
    public required string Description { get; set; }

    public required string MarkdownContent { get; set; }

    [Column(TypeName = "jsonb")]
    public GuideRichData? RichBlocks { get; set; }
    
    // Icon for the guide, typically a Minecraft Item ID
    public string? IconItemName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GuideRichData
{
    public GreenhouseLayout? GreenhouseLayout { get; set; }
}

public class GreenhouseLayout
{
    public int LayoutId { get; set; }
    public List<GreenhouseSlot> Slots { get; set; } = [];
}

public class GreenhouseSlot
{
    public int Index { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string? BackgroundTexture { get; set; }
}
