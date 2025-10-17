using EliteAPI.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Confirmations.Models;

public class UserConfirmation
{
	public string? UserId { get; set; }
	public ApiUser? User { get; set; }
    
	public int ConfirmationId { get; set; }
	public Confirmation? Confirmation { get; set; }
    
	public DateTimeOffset ConfirmedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class UserConfirmationEntityConfiguration : IEntityTypeConfiguration<UserConfirmation>
{
	public void Configure(EntityTypeBuilder<UserConfirmation> builder) {
		builder.HasKey(uc => new { uc.UserId, uc.ConfirmationId });
		
		builder.HasOne(uc => uc.User)
			.WithMany()
			.HasForeignKey(uc => uc.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}