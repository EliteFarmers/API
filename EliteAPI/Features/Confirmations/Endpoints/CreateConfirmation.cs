using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Confirmations.Models;
using FastEndpoints;

namespace EliteAPI.Features.Confirmations.Endpoints;

public class CreateConfirmationDto
{
	public required string Title { get; set; }
	public required string Content { get; set; }
	public bool IsActive { get; set; }
}

public class CreateConfirmationEndpoint(DataContext context) : Endpoint<CreateConfirmationDto, ConfirmationDto>
{
	public override void Configure()
	{
		Post("/admin/confirmations");
		Policies(ApiUserPolicies.Admin);
		
		Summary(s => {
			s.Summary = "Create a new confirmation";
			s.Description = "Creates a new confirmation that users will need to accept.";
		});
	}
    
	public override async Task HandleAsync(CreateConfirmationDto req, CancellationToken ct)
	{
		var confirmation = new Confirmation
		{
			Title = req.Title,
			Content = req.Content,
			IsActive = req.IsActive
		};
        
		await context.Confirmations.AddAsync(confirmation, ct);
		await context.SaveChangesAsync(ct);
        
		await Send.OkAsync(new ConfirmationDto
		{
			Id = confirmation.Id,
			Title = confirmation.Title,
			Content = confirmation.Content,
			IsActive = confirmation.IsActive,
			CreatedAt = confirmation.CreatedAt
		}, cancellation: ct);
	}
}