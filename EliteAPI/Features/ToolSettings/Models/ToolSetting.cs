using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using EliteAPI.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.ToolSettings.Models;

public class ToolSetting
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[MaxLength(256)]
	public string OwnerId { get; set; } = string.Empty;
	public ApiUser Owner { get; set; } = null!;

	[MaxLength(128)]
	public string TargetId { get; set; } = string.Empty;

	public int Version { get; set; } = 1;

	[MaxLength(128)]
	public string? Name { get; set; }

	[MaxLength(512)]
	public string? Description { get; set; }

	public bool IsPublic { get; set; } = false;

	[Column(TypeName = "jsonb")]
	public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ToolSettingEntityConfiguration : IEntityTypeConfiguration<ToolSetting>
{
	public void Configure(EntityTypeBuilder<ToolSetting> builder) {
		builder.Property(x => x.TargetId).HasMaxLength(128).IsRequired();
		builder.Property(x => x.Version).IsRequired();
		builder.Property(x => x.Name).HasMaxLength(128);
		builder.Property(x => x.Description).HasMaxLength(512);
		builder.Property(x => x.Data).IsRequired();
		builder.Property(x => x.OwnerId).IsRequired();

		builder.HasIndex(x => x.OwnerId);
		builder.HasIndex(x => new { x.OwnerId, x.TargetId });
		builder.HasIndex(x => new { x.TargetId, x.IsPublic });

		builder.HasOne(x => x.Owner)
			.WithMany()
			.HasForeignKey(x => x.OwnerId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
