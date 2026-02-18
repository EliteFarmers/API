using EliteAPI.Authentication;
using EliteAPI.Features.ToolSettings.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.ToolSettings.Endpoints;

public class DeleteToolSettingEndpoint(IToolSettingService toolSettingService) : EndpointWithoutRequest
{
	public override void Configure() {
		Delete("/tool-settings/{SettingId}");
		Options(x => x.AddEndpointFilter<WebsiteSecretOnlyFilter>());
		Version(0);

		Summary(s => {
			s.Summary = "Delete a tool setting";
			s.Description = "Delete a setting by sqid. Only the owner can delete.";
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

		var deleted = await toolSettingService.DeleteAsync(settingId, userId, ct);
		if (!deleted) {
			await Send.NotFoundAsync(ct);
			return;
		}

		await Send.NoContentAsync(ct);
	}
}
