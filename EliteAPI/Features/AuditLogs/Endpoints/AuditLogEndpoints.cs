using EliteAPI.Features.AuditLogs.Services;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;

namespace EliteAPI.Features.AuditLogs.Endpoints;

public class GetAuditLogsEndpoint(AuditLogService auditLogService)
    : Endpoint<GetAuditLogsRequest, GetAuditLogsResponse>
{
    public override void Configure()
    {
        Get("/admin/audit-logs");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Get admin audit logs";
            s.Description = "Retrieve paginated and filterable audit logs for administrative actions.";
        });
    }

    public override async Task HandleAsync(GetAuditLogsRequest req, CancellationToken ct)
    {
        var (logs, totalCount) = await auditLogService.GetLogsAsync(
            req.Offset,
            req.Limit,
            req.Action,
            req.TargetType,
            req.AdminUserId,
            req.FromDate,
            req.ToDate);

        await Send.OkAsync(new GetAuditLogsResponse
        {
            Logs = logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                AdminUserId = l.AdminUserId.ToString(),
                AdminUserName = l.AdminUser.MinecraftAccounts
                    .FirstOrDefault()?.Name ?? l.AdminUserId.ToString(),
                Action = l.Action,
                TargetType = l.TargetType,
                TargetId = l.TargetId,
                Details = l.Details,
                CreatedAt = l.CreatedAt,
                Data = l.Data
            }).ToList(),
            TotalCount = totalCount
        }, ct);
    }
}

public class GetAuditLogsRequest
{
    [QueryParam] public int Offset { get; set; } = 0;
    [QueryParam] public int Limit { get; set; } = 50;
    [QueryParam] public string? Action { get; set; }
    [QueryParam] public string? TargetType { get; set; }
    [QueryParam] public string? AdminUserId { get; set; }
    [QueryParam] public DateTime? FromDate { get; set; }
    [QueryParam] public DateTime? ToDate { get; set; }
}

public class GetAuditLogsResponse
{
    public List<AuditLogDto> Logs { get; set; } = [];
    public int TotalCount { get; set; }
}

public class AuditLogDto
{
    public long Id { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string AdminUserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public class GetAuditLogFiltersEndpoint(AuditLogService auditLogService)
    : EndpointWithoutRequest<AuditLogFiltersResponse>
{
    public override void Configure()
    {
        Get("/admin/audit-logs/filters");
        Policies(ApiUserPolicies.Moderator);
        Summary(s =>
        {
            s.Summary = "Get available audit log filters";
            s.Description = "Returns distinct actions and target types for filter dropdowns.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var actions = await auditLogService.GetDistinctActionsAsync();
        var targetTypes = await auditLogService.GetDistinctTargetTypesAsync();

        await Send.OkAsync(new AuditLogFiltersResponse
        {
            Actions = actions,
            TargetTypes = targetTypes
        }, ct);
    }
}

public class AuditLogFiltersResponse
{
    public List<string> Actions { get; set; } = [];
    public List<string> TargetTypes { get; set; } = [];
}
