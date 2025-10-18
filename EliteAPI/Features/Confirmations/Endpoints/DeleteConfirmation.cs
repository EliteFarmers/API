using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Confirmations.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Confirmations.Endpoints;

public class DeleteConfirmationDto
{
	public int Id { get; set; }
}

public class DeleteConfirmationEndpoint(DataContext context) : Endpoint<DeleteConfirmationDto, ConfirmationDto>
{
	public override void Configure()
	{
		Delete("/admin/confirmations/{Id}");
		Policies(ApiUserPolicies.Admin);
		
		Summary(s => {
			s.Summary = "Delete a login confirmation";
		});
	}
    
	public override async Task HandleAsync(DeleteConfirmationDto req, CancellationToken ct)
	{
		var confirmation = await context.Confirmations.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        
		if (confirmation is null)
		{
			await Send.NotFoundAsync(ct);
			return;
		}
        
		context.Confirmations.Remove(confirmation);
		await context.SaveChangesAsync(ct);
        
		await Send.OkAsync(ct);
	}
}