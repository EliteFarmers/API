using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Images.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Account.Models;

public class Badge
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[MaxLength(50)] public required string Name { get; set; }
	[MaxLength(1024)] public required string Description { get; set; }
	[MaxLength(512)] public required string Requirements { get; set; }

	[ForeignKey("Image")] [MaxLength(48)] public string? ImageId { get; set; }
	public Image? Image { get; set; }

	public bool TieToAccount { get; set; }
}

public class BadgeEntityConfiguration : IEntityTypeConfiguration<Badge>
{
	public void Configure(EntityTypeBuilder<Badge> builder) {
		builder.Navigation(b => b.Image).AutoInclude();
	}
}

public class UserBadge
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public bool Visible { get; set; } = true;
	public int Order { get; set; }
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

	[ForeignKey("Badge")] public int BadgeId { get; set; }
	public Badge Badge { get; set; } = null!;

	[ForeignKey("User")] [MaxLength(36)] public required string MinecraftAccountId { get; set; }
	public MinecraftAccount MinecraftAccount { get; set; } = null!;
}