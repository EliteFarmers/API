using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Confirmations.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Confirmations.Endpoints;

public class GetAllConfirmationsEndpoint(DataContext context) : EndpointWithoutRequest<List<ConfirmationDto>>
{
	public override void Configure()
	{
		Get("/admin/confirmations");
		Policies(ApiUserPolicies.Admin);
		
		Summary(s => {
			s.Summary = "Get a confirmation";
			s.Description = "Gets a confirmation that users will need to accept.";
		});
	}
    
	public override async Task HandleAsync(CancellationToken ct)
	{
		var confirmations = await context.Confirmations
			.Select(x => new ConfirmationDto
			{
				Id = x.Id,
				Title = x.Title,
				Content = x.Content,
				IsActive = x.IsActive,
				CreatedAt = x.CreatedAt
			})
			.ToListAsync(ct);
		
		await Send.OkAsync(confirmations, cancellation: ct);
	}
}