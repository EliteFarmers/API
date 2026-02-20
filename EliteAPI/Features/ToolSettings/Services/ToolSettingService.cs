using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Features.Common.Services;
using EliteAPI.Features.ToolSettings.Models;
using FastEndpoints;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.ToolSettings.Services;

public interface IToolSettingService
{
	Task<ToolSetting> CreateAsync(string ownerId, string targetId, int version, string? name, string? description,
		JsonElement data, bool isPublic, CancellationToken ct);
	Task<ToolSetting?> GetBySqidAsync(string sqid, string requesterId, CancellationToken ct);
	Task<List<ToolSetting>> ListByOwnerAsync(string ownerId, string? targetId, bool? isPublic, int limit, int offset,
		CancellationToken ct);
	Task<ToolSetting?> UpdateAsync(string sqid, string ownerId, string targetId, int version, string? name,
		string? description, JsonElement data, bool isPublic, CancellationToken ct);
	Task<bool> DeleteAsync(string sqid, string ownerId, CancellationToken ct);
	string GetSqid(int id);
}

[RegisterService<IToolSettingService>(LifeTime.Scoped)]
public class ToolSettingService(DataContext db, HtmlSanitizer sanitizer) : IToolSettingService
{
	public async Task<ToolSetting> CreateAsync(string ownerId, string targetId, int version, string? name,
		string? description, JsonElement data, bool isPublic, CancellationToken ct) {
		var now = DateTime.UtcNow;
		var setting = new ToolSetting {
			OwnerId = ownerId,
			TargetId = targetId,
			Version = version,
			Name = SanitizeText(name),
			Description = SanitizeText(description),
			IsPublic = isPublic,
			Data = ConvertToJsonDocument(data),
			CreatedAt = now,
			UpdatedAt = now
		};

		db.ToolSettings.Add(setting);
		await db.SaveChangesAsync(ct);
		return setting;
	}

	public async Task<ToolSetting?> GetBySqidAsync(string sqid, string requesterId, CancellationToken ct) {
		var id = SqidService.Decode(sqid);
		if (id is null)
			return null;

		var setting = await db.ToolSettings
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id.Value, ct);

		if (setting is null)
			return null;

		if (!setting.IsPublic && setting.OwnerId != requesterId)
			return null;

		return setting;
	}

	public async Task<List<ToolSetting>> ListByOwnerAsync(string ownerId, string? targetId, bool? isPublic, int limit,
		int offset, CancellationToken ct) {
		var normalizedLimit = Math.Clamp(limit, 1, 100);
		var normalizedOffset = Math.Max(offset, 0);

		var query = db.ToolSettings
			.AsNoTracking()
			.Where(x => x.OwnerId == ownerId);

		if (!string.IsNullOrWhiteSpace(targetId))
			query = query.Where(x => x.TargetId == targetId);

		if (isPublic.HasValue)
			query = query.Where(x => x.IsPublic == isPublic.Value);

		return await query
			.OrderByDescending(x => x.UpdatedAt)
			.Skip(normalizedOffset)
			.Take(normalizedLimit)
			.ToListAsync(ct);
	}

	public async Task<ToolSetting?> UpdateAsync(string sqid, string ownerId, string targetId, int version, string? name,
		string? description, JsonElement data, bool isPublic, CancellationToken ct) {
		var id = SqidService.Decode(sqid);
		if (id is null)
			return null;

		var setting = await db.ToolSettings.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
		if (setting is null || setting.OwnerId != ownerId)
			return null;

		setting.TargetId = targetId;
		setting.Version = version;
		setting.Name = SanitizeText(name);
		setting.Description = SanitizeText(description);
		setting.IsPublic = isPublic;
		setting.Data.Dispose();
		setting.Data = ConvertToJsonDocument(data);
		setting.UpdatedAt = DateTime.UtcNow;

		await db.SaveChangesAsync(ct);
		return setting;
	}

	public async Task<bool> DeleteAsync(string sqid, string ownerId, CancellationToken ct) {
		var id = SqidService.Decode(sqid);
		if (id is null)
			return false;

		var setting = await db.ToolSettings.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
		if (setting is null || setting.OwnerId != ownerId)
			return false;

		db.ToolSettings.Remove(setting);
		await db.SaveChangesAsync(ct);
		return true;
	}

	public string GetSqid(int id) => SqidService.Encode(id);

	private JsonDocument ConvertToJsonDocument(JsonElement data) {
		return ToolSettingsJsonGuard.SanitizeStrings(data, sanitizer);
	}

	private string? SanitizeText(string? value) {
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var sanitized = sanitizer.Sanitize(value.Trim());
		return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
	}
}
