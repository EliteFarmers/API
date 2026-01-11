using EliteAPI.Data;
using EliteAPI.Features.AuditLogs.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.AuditLogs.Services;

[RegisterService<AuditLogService>(LifeTime.Scoped)]
public class AuditLogService(DataContext db, IMessageService messageService)
{
    public async Task LogAsync(
        ulong adminUserId,
        string action,
        string targetType,
        string? targetId = null,
        string? details = null,
        Dictionary<string, object>? data = null)
    {
        var log = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            Data = data
        };

        db.AdminAuditLogs.Add(log);
        await db.SaveChangesAsync();

        messageService.SendAuditLogMessage(
            adminUserId.ToString(),
            action,
            targetType,
            targetId,
            details);
    }

    public async Task<(List<AdminAuditLog> Logs, int TotalCount)> GetLogsAsync(
        int offset = 0,
        int limit = 50,
        string? action = null,
        string? targetType = null,
        ulong? adminUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = db.AdminAuditLogs
            .Include(l => l.AdminUser)
                .ThenInclude(a => a.MinecraftAccounts)
            .AsQueryable();

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (!string.IsNullOrEmpty(targetType))
        {
            query = query.Where(l => l.TargetType == targetType);
        }

        if (adminUserId.HasValue)
        {
            query = query.Where(l => l.AdminUserId == adminUserId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<string>> GetDistinctActionsAsync()
    {
        return await db.AdminAuditLogs
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctTargetTypesAsync()
    {
        return await db.AdminAuditLogs
            .Select(l => l.TargetType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }
}
