using System.Net;
using EliteAPI.Features.AuditLogs.Endpoints;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class AuditLogEndpointTests(GuideTestApp App) : TestBase
{
    [Fact, Priority(1)]
    public async Task GetAuditLogs_Anonymous_Returns401()
    {
        var rsp = await App.AnonymousClient.GetAsync("/admin/audit-logs");
        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(2)]
    public async Task GetAuditLogs_RegularUser_Returns403()
    {
        var rsp = await App.RegularUserClient.GetAsync("/admin/audit-logs");
        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(3)]
    public async Task GetAuditLogs_AsModerator_Succeeds()
    {
        var (rsp, res) = await App.ModeratorClient.GETAsync<GetAuditLogsEndpoint, GetAuditLogsRequest, GetAuditLogsResponse>(
            new GetAuditLogsRequest());
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Logs.ShouldNotBeNull();
    }

    [Fact, Priority(4)]
    public async Task GetAuditLogs_AsAdmin_Succeeds()
    {
        var (rsp, res) = await App.AdminClient.GETAsync<GetAuditLogsEndpoint, GetAuditLogsRequest, GetAuditLogsResponse>(
            new GetAuditLogsRequest());
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Logs.ShouldNotBeNull();
    }

    [Fact, Priority(5)]
    public async Task GetAuditLogs_WithPagination_ReturnsPaginatedList()
    {
        var (rsp, res) = await App.ModeratorClient.GETAsync<GetAuditLogsEndpoint, GetAuditLogsRequest, GetAuditLogsResponse>(
            new GetAuditLogsRequest { Offset = 0, Limit = 10 });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.TotalCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact, Priority(6)]
    public async Task GetAuditLogs_WithActionFilter_ReturnsFilteredList()
    {
        var (rsp, res) = await App.ModeratorClient.GETAsync<GetAuditLogsEndpoint, GetAuditLogsRequest, GetAuditLogsResponse>(
            new GetAuditLogsRequest { Action = "guide_approved" });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
    }

    [Fact, Priority(7)]
    public async Task GetAuditLogs_WithTargetTypeFilter_ReturnsFilteredList()
    {
        var (rsp, res) = await App.ModeratorClient.GETAsync<GetAuditLogsEndpoint, GetAuditLogsRequest, GetAuditLogsResponse>(
            new GetAuditLogsRequest { TargetType = "Guide" });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
    }

    [Fact, Priority(8)]
    public async Task GetAuditLogFilters_AsModerator_Succeeds()
    {
        var (rsp, res) = await App.ModeratorClient.GETAsync<GetAuditLogFiltersEndpoint, AuditLogFiltersResponse>();
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Actions.ShouldNotBeNull();
        res.TargetTypes.ShouldNotBeNull();
    }

    [Fact, Priority(9)]
    public async Task GetAuditLogFilters_RegularUser_Returns403()
    {
        var rsp = await App.RegularUserClient.GetAsync("/admin/audit-logs/filters");
        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
