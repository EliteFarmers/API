using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.AuditLogs.Models;

public class AdminAuditLog
{
    [Key]
    public long Id { get; set; }

    public required ulong AdminUserId { get; set; }
    public EliteAccount AdminUser { get; set; } = null!;

    [MaxLength(128)]
    public required string Action { get; set; }

    [MaxLength(64)]
    public required string TargetType { get; set; }

    [MaxLength(128)]
    public string? TargetId { get; set; }

    [MaxLength(2048)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Data { get; set; }
}

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.HasIndex(l => l.AdminUserId);
        builder.HasIndex(l => l.Action);
        builder.HasIndex(l => l.TargetType);
        builder.HasIndex(l => l.CreatedAt);

        builder.HasOne(l => l.AdminUser)
            .WithMany()
            .HasForeignKey(l => l.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
