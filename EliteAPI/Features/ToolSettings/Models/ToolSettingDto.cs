using System.Text.Json;
using EliteAPI.Features.ToolSettings.Services;

namespace EliteAPI.Features.ToolSettings.Models;

public class ToolSettingDto
{
	public string Id { get; set; } = string.Empty;
	public string OwnerId { get; set; } = string.Empty;
	public string TargetId { get; set; } = string.Empty;
	public bool IsPublic { get; set; }
	public JsonElement Data { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public static ToolSettingDto FromEntity(ToolSetting setting, IToolSettingService toolSettingService) {
		return new ToolSettingDto {
			Id = toolSettingService.GetSqid(setting.Id),
			OwnerId = setting.OwnerId,
			TargetId = setting.TargetId,
			IsPublic = setting.IsPublic,
			Data = setting.Data.RootElement.Clone(),
			CreatedAt = setting.CreatedAt,
			UpdatedAt = setting.UpdatedAt
		};
	}
}
