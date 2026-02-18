using System.Text.Json;
using EliteAPI.Authentication;
using EliteAPI.Features.ToolSettings.Models;
using EliteAPI.Features.ToolSettings.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.ToolSettings.Endpoints;

public class CreateToolSettingEndpoint(IToolSettingService toolSettingService)
	: Endpoint<CreateToolSettingRequest, ToolSettingDto>
{
	public override void Configure() {
		Post("/tool-settings");
		Options(x => x.AddEndpointFilter<WebsiteSecretOnlyFilter>());
		Version(0);

		Summary(s => {
			s.Summary = "Create a tool setting";
			s.Description = "Create a new tool setting entry with optional public visibility.";
		});
	}

	public override async Task HandleAsync(CreateToolSettingRequest req, CancellationToken ct) {
		var userId = User.GetId();
		if (string.IsNullOrWhiteSpace(userId)) {
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var setting = await toolSettingService.CreateAsync(userId, req.TargetId.Trim(), req.Data, req.IsPublic, ct);
		await Send.OkAsync(ToolSettingDto.FromEntity(setting, toolSettingService), ct);
	}
}

public class CreateToolSettingRequest
{
	public string TargetId { get; set; } = string.Empty;
	public bool IsPublic { get; set; }
	public JsonElement Data { get; set; }
}

public class CreateToolSettingRequestValidator : Validator<CreateToolSettingRequest>
{
	public CreateToolSettingRequestValidator() {
		RuleFor(x => x.TargetId).NotEmpty().MaximumLength(128);
		RuleFor(x => x.Data)
			.Must(x => x.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
			.WithMessage("Data is required.");
	}
}
