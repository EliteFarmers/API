using EliteAPI.Authentication;
using EliteAPI.Features.ToolSettings.Models;
using EliteAPI.Features.ToolSettings.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.ToolSettings.Endpoints;

public class GetToolSettingEndpoint(IToolSettingService toolSettingService) : EndpointWithoutRequest<ToolSettingDto>
{
	public override void Configure() {
		Get("/tool-settings/{SettingId}");
		Options(x => x.AddEndpointFilter<WebsiteSecretOnlyFilter>());
		Version(0);

		Summary(s => {
			s.Summary = "Get a tool setting";
			s.Description = "Get a setting by sqid. Public settings can be read by any authenticated user.";
		});
	}

	public override async Task HandleAsync(CancellationToken ct) {
		var userId = User.GetId();
		if (string.IsNullOrWhiteSpace(userId)) {
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var settingId = Route<string>("SettingId");
		if (string.IsNullOrWhiteSpace(settingId)) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var setting = await toolSettingService.GetBySqidAsync(settingId, userId, ct);
		if (setting is null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.OkAsync(ToolSettingDto.FromEntity(setting, toolSettingService), ct);
	}
}
