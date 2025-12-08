using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Recap.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Recap.Endpoints;

public class ToggleRecapVisibilityEndpoint(IYearlyRecapService recapService, IAccountService accountService)
	: Endpoint<ToggleRecapVisibilityRequest>
{
	public override void Configure() {
		Post("/recap/{Year}/player/{PlayerUuid}/{ProfileUuid}/visibility");
		Version(0);

		Summary(s => {
			s.Summary = "Toggle Recap Visibility";
			s.Description = "Toggles the public visibility of a player's yearly recap.";
		});
	}

	public override async Task HandleAsync(ToggleRecapVisibilityRequest request, CancellationToken ct) {
		if (!recapService.ValidYear(request.Year)) {
			ThrowError("Invalid year specified.", StatusCodes.Status400BadRequest);
		}
		
		var hasAccess = await accountService.OwnsMinecraftAccount(User, request.PlayerUuidFormatted);
		if (!hasAccess) {
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var success = await recapService.TogglePublicStatusAsync(request.PlayerUuidFormatted,
			request.ProfileUuidFormatted, request.Year, request.Body.Public);

		if (!success) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.NoContentAsync(ct);
	}
}

public class ToggleRecapVisibilityRequest : PlayerProfileUuidRequest
{
	public int Year { get; set; }

	[FromBody] public ToggleRecapVisibilityRequestBody Body { get; set; } = new();
}

public class ToggleRecapVisibilityRequestBody
{
	public bool Public { get; set; }
}

public class ToggleRecapVisibilityRequestValidator : Validator<ToggleRecapVisibilityRequest>
{
	public ToggleRecapVisibilityRequestValidator() {
		Include(new PlayerProfileUuidRequestValidator());
		RuleFor(x => x.Year).GreaterThan(2000).LessThan(3000);
	}
}