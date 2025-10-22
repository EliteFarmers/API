using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Confirmations.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Confirmations.Endpoints;

public class UpdateConfirmationRequest
{
	public int Id { get; set; }
	public string? Title { get; set; }
	public string? Content { get; set; }
	public bool IsActive { get; set; }
}

public class UpdateConfirmationEndpoint(DataContext context) : Endpoint<UpdateConfirmationRequest, ConfirmationDto>
{
	public override void Configure()
	{
		Put("/admin/confirmations/{Id}");
		Policies(ApiUserPolicies.Admin);
		
		Summary(s => {
			s.Summary = "Update a confirmation";
			s.Description = "Updates a confirmation that users will need to accept.";
		});
	}
    
	public override async Task HandleAsync(UpdateConfirmationRequest req, CancellationToken ct)
	{
		var confirmation = await context.Confirmations.FirstOrDefaultAsync(x => x.Id == req.Id, ct);

		if (confirmation is null) {
			await Send.NotFoundAsync(ct);
			return;
		}
		
		confirmation.Title = req.Title ?? confirmation.Title;
		confirmation.Content = req.Content ?? confirmation.Content;
		confirmation.IsActive = req.IsActive;
        
		await context.SaveChangesAsync(ct);
        
		await Send.OkAsync(ct);
	}
}