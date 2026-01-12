using EliteAPI.Data;
using EliteAPI.Features.Notifications.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Notifications.Services;

[RegisterService<NotificationService>(LifeTime.Scoped)]
public class NotificationService(DataContext db)
{
    public async Task CreateAsync(
        ulong userId,
        NotificationType type,
        string title,
        string? message = null,
        string? link = null,
        Dictionary<string, object>? data = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            Data = data
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(
        ulong userId,
        int offset = 0,
        int limit = 20,
        bool unreadOnly = false)
    {
        var query = db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        if (unreadOnly)
        {
            query = (IOrderedQueryable<Notification>)query.Where(n => !n.IsRead);
        }

        return await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(ulong userId)
    {
        return await db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, ulong userId)
    {
        var affected = await db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return affected > 0;
    }

    public async Task<int> MarkAllAsReadAsync(ulong userId)
    {
        return await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task<bool> DeleteAsync(long notificationId, ulong userId)
    {
        var affected = await db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteDeleteAsync();

        return affected > 0;
    }
}
