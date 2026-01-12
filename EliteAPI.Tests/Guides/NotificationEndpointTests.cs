using System.Net;
using System.Net.Http.Json;
using EliteAPI.Features.Notifications.Endpoints;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class NotificationEndpointTests(GuideTestApp App) : TestBase
{
    [Fact, Priority(1)]
    public async Task GetNotifications_Unauthenticated_Returns401()
    {
        var rsp = await App.AnonymousClient.GetAsync("/notifications");
        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(2)]
    public async Task GetNotifications_Authenticated_ReturnsEmptyList()
    {
        var (rsp, res) = await App.RegularUserClient.GETAsync<GetNotificationsEndpoint, GetNotificationsRequest, GetNotificationsResponse>(
            new GetNotificationsRequest());
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Notifications.ShouldNotBeNull();
    }

    [Fact, Priority(3)]
    public async Task MarkNotificationRead_InvalidId_Returns404()
    {
        var rsp = await App.RegularUserClient.PostAsync("/notifications/999999/read", null);
        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(4)]
    public async Task MarkAllNotificationsRead_Returns200()
    {
        var rsp = await App.RegularUserClient.PostAsync("/notifications/read-all", null);
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact, Priority(5)]
    public async Task DeleteNotification_InvalidId_Returns404()
    {
        var rsp = await App.RegularUserClient.DeleteAsync("/notifications/999999");
        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(6)]
    public async Task GetNotifications_WithUnreadOnly_ReturnsFilteredList()
    {
        var (rsp, res) = await App.RegularUserClient.GETAsync<GetNotificationsEndpoint, GetNotificationsRequest, GetNotificationsResponse>(
            new GetNotificationsRequest { UnreadOnly = true });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Notifications.ShouldNotBeNull();
    }

    [Fact, Priority(7)]
    public async Task GetNotifications_WithPagination_ReturnsPaginatedList()
    {
        var (rsp, res) = await App.RegularUserClient.GETAsync<GetNotificationsEndpoint, GetNotificationsRequest, GetNotificationsResponse>(
            new GetNotificationsRequest { Offset = 0, Limit = 5 });
        
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldNotBeNull();
        res.Notifications.ShouldNotBeNull();
    }
}
