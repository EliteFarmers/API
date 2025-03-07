using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Badges.AddBadgeToUser;

internal sealed class AddBadgeToUserBadgeEndpoint(
	IBadgeService badgeService
	) : Endpoint<PlayerBadgeRequest> 
{
	public override void Configure() {
		Post("/badge/user/{Player}/{BadgeId}");
		Policies(ApiUserPolicies.Moderator);
		Version(0);
		
		Description(s => s.Accepts<PlayerBadgeRequest>());
		
		Summary(s => {
			s.Summary = "Add a badge to a user";
		});
	}

	public override async Task HandleAsync(PlayerBadgeRequest request, CancellationToken c) 
	{
		var result = await badgeService.AddBadgeToUser(request.Player, request.BadgeId);
		
		switch (result) {
			case BadRequestObjectResult badRequest:
				ThrowError(badRequest.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
				break;
			case NotFoundObjectResult notFound:
				ThrowError(notFound.Value?.ToString() ?? "Not found", StatusCodes.Status404NotFound);
				break;
		}

		await SendNoContentAsync(cancellation: c);
	}
}