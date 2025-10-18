using EliteAPI.Data;
using EliteAPI.Features.Confirmations.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Auth;

public class GetConfirmationDto
{
	public int Id { get; set; }
}

public class GetConfirmationEndpoint(DataContext context) : Endpoint<GetConfirmationDto, ConfirmationDto>
{
	public override void Configure()
	{
		Get("/auth/confirmations/{Id}");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get a confirmation";
			s.Description = "Gets a confirmation that users will need to accept.";
		});
		
		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(2))));
	}
    
	public override async Task HandleAsync(GetConfirmationDto req, CancellationToken ct)
	{
		var confirmation = await context.Confirmations.FirstOrDefaultAsync(x => x.Id == req.Id, ct);

		if (confirmation is null) {
			await Send.NotFoundAsync(ct);
			return;
		}

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