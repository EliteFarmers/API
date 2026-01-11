using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Notifications.Models;

public class Notification
{
    [Key]
    public long Id { get; set; }

    public required ulong UserId { get; set; }
    public EliteAccount User { get; set; } = null!;

    public NotificationType Type { get; set; }

    [MaxLength(256)]
    public required string Title { get; set; }

    [MaxLength(2048)]
    public string? Message { get; set; }

    [MaxLength(512)]
    public string? Link { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Data { get; set; }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
