using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole> 
{
	public void Configure(EntityTypeBuilder<IdentityRole> builder) {
		builder.HasData(
			new IdentityRole {
				Id = "8270a1b1-5809-436a-ba1c-b712f4f55f67",
				Name = "Admin",
				NormalizedName = "ADMIN"
			},
			new IdentityRole {
				Id = "3384aba1-5453-4787-81d9-0b7222225d81",
				Name = "Moderator",
				NormalizedName = "MODERATOR"
			},
			new IdentityRole {
				Id = "d8c803c1-63a0-4594-8d68-aad7bd59df7d",
				Name = "Support",
				NormalizedName = "SUPPORT"
			},
			new IdentityRole {
				Id = "ff4f5319-644e-4332-8bd5-2ec989ba5e7f",
				Name = "Wiki",
				NormalizedName = "WIKI"
			},
			new IdentityRole {
				Id = "e99efab5-3fd2-416e-b8f5-93b0370892ac",
				Name = "User",
				NormalizedName = "USER"
			}
		);
	}
}