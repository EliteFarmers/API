using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Auth.Models;

public class RefreshToken {
	[Key] public int Id { get; set; }

	[Required] public required string UserId { get; set; } = null!;

	[Required] public required string Token { get; set; } = null!;

	[Required] public DateTime ExpiresUtc { get; set; }

	[Required] public DateTime CreatedUtc { get; set; }
	public DateTime? RevokedUtc { get; set; }

	public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
	public bool IsRevoked => RevokedUtc != null;
	public bool IsActive => !IsRevoked && !IsExpired;

	[ForeignKey(nameof(UserId))] public virtual ApiUser User { get; set; } = null!;
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken> {
	public void Configure(EntityTypeBuilder<RefreshToken> builder) {
		builder.HasKey(e => e.Id);
		builder.Property(e => e.Id).UseIdentityAlwaysColumn();

		builder.HasIndex(e => new { e.UserId, e.Token });

		builder.Property(e => e.UserId).IsRequired();
		builder.Property(e => e.Token).IsRequired();
		builder.Property(e => e.ExpiresUtc).IsRequired();
		builder.Property(e => e.CreatedUtc).IsRequired();

		builder.HasOne(e => e.User)
			.WithMany()
			.HasForeignKey(e => e.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}