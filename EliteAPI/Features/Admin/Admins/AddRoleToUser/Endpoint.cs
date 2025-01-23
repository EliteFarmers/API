using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Admins.AddRoleToUser;

internal sealed class AddRoleToUserEndpoint(
	DataContext context,
	UserManager userManager)
	: Endpoint<UserRoleRequest> 
{
	public override void Configure() {
		Post("/admin/user/{DiscordId}/roles/{Role}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Add a role to a user";
		});
	}

	public override async Task HandleAsync(UserRoleRequest request, CancellationToken c) 
	{
		// Check that the role exists in the role database
		var existingRole = await context.Roles.AsNoTracking()
			.FirstOrDefaultAsync(r => r.Name == request.Role, cancellationToken: c);
        
		if (existingRole is null) {
			ThrowError("Role not found", StatusCodes.Status400BadRequest);
		}
        
		var user = await userManager.FindByIdAsync(request.DiscordId.ToString());

		if (user is null) {
			ThrowError("User not found", StatusCodes.Status400BadRequest);
		}
        
		// Add role to user
		var result = await userManager.AddToRoleAsync(user, request.Role);
        
		if (!result.Succeeded) {
			ThrowError("Failed to add role");
		}

		await SendNoContentAsync(cancellation: c);
	}
}