using EliteAPI.Features.Notifications.Models;
using EliteAPI.Features.Notifications.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Notifications.Endpoints;

public class GetNotificationsEndpoint(NotificationService notificationService) 
    : Endpoint<GetNotificationsRequest, GetNotificationsResponse>
{
    public override void Configure()
    {
        Get("/notifications");
        
        Options(x => x.Accepts<GetNotificationsRequest>());
        
        Summary(s =>
        {
            s.Summary = "Get user notifications";
            s.Description = "Retrieve paginated notifications for the authenticated user.";
        });
    }

    public override async Task HandleAsync(GetNotificationsRequest req, CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var notifications = await notificationService.GetUserNotificationsAsync(
            userId.Value, 
            req.Offset, 
            req.Limit, 
            req.UnreadOnly);

        var unreadCount = await notificationService.GetUnreadCountAsync(userId.Value);

        await Send.OkAsync(new GetNotificationsResponse
        {
            Notifications = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                Link = n.Link,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                Data = n.Data
            }).ToList(),
            UnreadCount = unreadCount
        }, ct);
    }
}

public class GetNotificationsRequest
{
    [QueryParam] public int Offset { get; set; } = 0;
    [QueryParam] public int Limit { get; set; } = 20;
    [QueryParam] public bool UnreadOnly { get; set; } = false;
}

public class GetNotificationsResponse
{
    public List<NotificationDto> Notifications { get; set; } = [];
    public int UnreadCount { get; set; }
}

public class NotificationDto
{
    public long Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public class MarkNotificationReadEndpoint(NotificationService notificationService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/notifications/{Id}/read");
        Summary(s =>
        {
            s.Summary = "Mark notification as read";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var id = Route<long>("Id");
        var success = await notificationService.MarkAsReadAsync(id, userId.Value);

        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public class MarkAllNotificationsReadEndpoint(NotificationService notificationService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/notifications/read-all");
        Summary(s =>
        {
            s.Summary = "Mark all notifications as read";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var count = await notificationService.MarkAllAsReadAsync(userId.Value);
        await Send.OkAsync(new { MarkedAsRead = count }, ct);
    }
}

public class DeleteNotificationEndpoint(NotificationService notificationService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/notifications/{Id}");
        Summary(s =>
        {
            s.Summary = "Delete a notification";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetDiscordId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var id = Route<long>("Id");
        var success = await notificationService.DeleteAsync(id, userId.Value);

        if (!success)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
