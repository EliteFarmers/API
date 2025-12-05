using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Recap.Models;
using EliteAPI.Features.Recap.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Recap.Endpoints;

public class GetPlayerRecapEndpoint(IYearlyRecapService recapService, IAccountService accountService)
	: Endpoint<RecapRequest, YearlyRecapDto>
{
	public override void Configure() {
		Get("/recap/{Year}/player/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Player Recap";
			s.Description =
				"Retrieves the yearly recap for a player. Requires authentication if the recap is not public.";
		});
	}

	public override async Task HandleAsync(RecapRequest request, CancellationToken ct) {
		if (!recapService.ValidYear(request.Year)) {
			ThrowError("Invalid year specified.", StatusCodes.Status400BadRequest);
		}
		
		var isPublic = await recapService.IsPublicRecapAsync(request.PlayerUuidFormatted, request.ProfileUuidFormatted,
			request.Year);

		if (!isPublic) {
			var hasAccess = User.Identity?.IsAuthenticated is true
			                && await accountService.OwnsMinecraftAccount(User, request.PlayerUuidFormatted,
				                ApiUserPolicies.Moderator);

			if (!hasAccess) {
				await Send.UnauthorizedAsync(ct);
				return;
			}
		}

		var recap = await recapService.GetRecapAsync(request.PlayerUuidFormatted, request.ProfileUuidFormatted,
			request.Year);

		if (recap == null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.OkAsync(recap, cancellation: ct);
	}
}

public class RecapRequest : PlayerProfileUuidRequest
{
	public int Year { get; set; }
}

public class RecapRequestValidator : Validator<RecapRequest>
{
	public RecapRequestValidator()
	{
		Include(new PlayerProfileUuidRequestValidator());
		RuleFor(x => x.Year).GreaterThan(2000).LessThan(3000);
	}
}
