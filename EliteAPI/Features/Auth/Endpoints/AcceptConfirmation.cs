using EliteAPI.Data;
using EliteAPI.Features.Confirmations.Models;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Auth;

public class AcceptConfirmationRequest
{
	public int Id { get; set; }
}

public class AcceptConfirmationEndpoint(DataContext context, UserManager userManager) : Endpoint<AcceptConfirmationRequest, ConfirmationDto>
{
	public override void Configure()
	{
		Post("/auth/confirmations/{Id}/accept");
		
		Description(x => x.Accepts<AcceptConfirmationRequest>());
		
		Summary(s => {
			s.Summary = "Accept a confirmation";
			s.Description = "Accepts a login confirmation that users will need to accept to proceed.";
		});
	}
    
	public override async Task HandleAsync(AcceptConfirmationRequest req, CancellationToken ct) {
		var userId = User.GetId();
		if (userId is null) {
			await Send.UnauthorizedAsync(ct);
			return;
		}
		
		var user = await userManager.FindByIdAsync(userId);
		if (user is null) {
			await Send.UnauthorizedAsync(ct);
			return;
		}
		
		// Check if the user has already accepted this confirmation
		var alreadyAccepted = await context.UserConfirmations.AnyAsync(uc => uc.UserId == user.Id && uc.ConfirmationId == req.Id, ct);
		if (alreadyAccepted) {
			await Send.NoContentAsync(ct);
			return;
		}
		
		// Check if the confirmation exists and is active
		var confirmation = await context.Confirmations.FirstOrDefaultAsync(c => c.Id == req.Id && c.IsActive, ct);
		if (confirmation is null) {
			await Send.NotFoundAsync(ct);
			return;
		}
		
		var userConfirmation = new UserConfirmation
		{
			UserId = user.Id,
			ConfirmationId = confirmation.Id,
			ConfirmedAt = DateTimeOffset.UtcNow
		};

		try {
			await context.UserConfirmations.AddAsync(userConfirmation, ct);
			await context.SaveChangesAsync(ct);
		} catch (DbUpdateException) {
			// Catch unique constraint violation (user already accepted)
			await Send.NoContentAsync(ct);
			return;
		}
		
		await Send.NoContentAsync(ct);
	}
}