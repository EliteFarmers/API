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
		Version(0);
		
		Options(x => {
			x.AddEndpointFilter<WebsiteSecretOnlyFilter>();
		});
		MaxRequestBodySize(ToolSettingsJsonGuard.MaxRequestBodyBytes);

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

		var setting = await toolSettingService.CreateAsync(userId, req.TargetId.Trim(), req.Version, req.Name,
			req.Description, req.Data, req.IsPublic, ct);
		await Send.OkAsync(ToolSettingDto.FromEntity(setting, toolSettingService), ct);
	}
}

public class CreateToolSettingRequest
{
	public string TargetId { get; set; } = string.Empty;
	public int Version { get; set; } = 1;
	public string? Name { get; set; }
	public string? Description { get; set; }
	public bool IsPublic { get; set; }
	public JsonElement Data { get; set; }
}

public class CreateToolSettingRequestValidator : Validator<CreateToolSettingRequest>
{
	public CreateToolSettingRequestValidator() {
		RuleFor(x => x.TargetId).NotEmpty().MaximumLength(128);
		RuleFor(x => x.Name).MaximumLength(128).When(x => x.Name is not null);
		RuleFor(x => x.Description).MaximumLength(512).When(x => x.Description is not null);
		RuleFor(x => x.Data).Custom((data, context) => {
			if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) {
				context.AddFailure("Data is required.");
				return;
			}

			if (!ToolSettingsJsonGuard.TryValidate(data, out var error))
				context.AddFailure(error ?? "Data contains invalid JSON structure.");
		});
	}
}
