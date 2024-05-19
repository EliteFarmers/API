using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole> 
{
	public void Configure(EntityTypeBuilder<IdentityRole> builder) {
		builder.HasData(
			new IdentityRole {
				Name = "Admin",
				NormalizedName = "ADMIN"
			},
			new IdentityRole {
				Name = "Moderator",
				NormalizedName = "MODERATOR"
			},
			new IdentityRole {
				Name = "Support",
				NormalizedName = "SUPPORT"
			},
			new IdentityRole {
				Name = "Wiki",
				NormalizedName = "WIKI"
			},
			new IdentityRole {
				Name = "User",
				NormalizedName = "USER"
			}
		);
	}
}