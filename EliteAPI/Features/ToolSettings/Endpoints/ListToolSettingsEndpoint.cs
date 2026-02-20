using EliteAPI.Authentication;
using EliteAPI.Features.ToolSettings.Models;
using EliteAPI.Features.ToolSettings.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.ToolSettings.Endpoints;

public class ListToolSettingsEndpoint(IToolSettingService toolSettingService)
	: Endpoint<ListToolSettingsRequest, List<ToolSettingDto>>
{
	public override void Configure() {
		Get("/tool-settings");
		Options(x => x.AddEndpointFilter<WebsiteSecretOnlyFilter>());
		Version(0);

		Summary(s => {
			s.Summary = "List tool settings";
			s.Description = "List the authenticated user's settings with optional target/visibility filters.";
		});
	}

	public override async Task HandleAsync(ListToolSettingsRequest req, CancellationToken ct) {
		var userId = User.GetId();
		if (string.IsNullOrWhiteSpace(userId)) {
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var settings = await toolSettingService.ListByOwnerAsync(userId, req.TargetId, req.IsPublic, req.Limit,
			req.Offset, ct);

		await Send.OkAsync(settings.Select(x => ToolSettingDto.FromEntity(x, toolSettingService)).ToList(), ct);
	}
}

public class ListToolSettingsRequest
{
	[QueryParam] public string? TargetId { get; set; }
	[QueryParam] public bool? IsPublic { get; set; }
	[QueryParam] public int Limit { get; set; } = 25;
	[QueryParam] public int Offset { get; set; } = 0;
}

public class ListToolSettingsRequestValidator : Validator<ListToolSettingsRequest>
{
	public ListToolSettingsRequestValidator() {
		RuleFor(x => x.TargetId).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.TargetId));
		RuleFor(x => x.Limit).InclusiveBetween(1, 100);
		RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
	}
}
