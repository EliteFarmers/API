using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Badges.DeleteBadgeFromUser;

internal sealed class DeleteBadgeFromUserBadgeEndpoint(
	IBadgeService badgeService
	) : Endpoint<PlayerBadgeRequest> 
{
	public override void Configure() {
		Delete("/badge/user/{Player}/{BadgeId}");
		Policies(ApiUserPolicies.Moderator);
		Version(0);
		
		Summary(s => {
			s.Summary = "Remove a badge from a user";
		});
	}

	public override async Task HandleAsync(PlayerBadgeRequest request, CancellationToken c) 
	{
		var result = await badgeService.RemoveBadgeFromUser(request.Player, request.BadgeId);
		
		switch (result) {
			case BadRequestObjectResult badRequest:
				ThrowError(badRequest.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
				break;
			case NotFoundObjectResult notFound:
				ThrowError(notFound.Value?.ToString() ?? "Not found", StatusCodes.Status404NotFound);
				break;
		}

		await SendOkAsync(cancellation: c);
	}
}