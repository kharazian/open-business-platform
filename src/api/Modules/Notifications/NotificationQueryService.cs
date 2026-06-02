using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Notifications;

public sealed class NotificationQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public NotificationQueryService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<NotificationDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var notifications = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .ToArrayAsync(cancellationToken);

        return notifications.Select(ToDto).ToArray();
    }

    public async Task<NotificationUnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && notification.ReadAt == null, cancellationToken);

        return new NotificationUnreadCountDto(unreadCount);
    }

    public async Task<NotificationDto?> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(candidate => candidate.Id == notificationId && candidate.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return null;
        }

        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(notification);
    }

    public async Task<NotificationUnreadCountDto> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unreadNotifications = await dbContext.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAt == null)
            .ToArrayAsync(cancellationToken);

        if (unreadNotifications.Length > 0)
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var notification in unreadNotifications)
            {
                notification.ReadAt = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetUnreadCountAsync(userId, cancellationToken);
    }

    private static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.SourceType,
            notification.SourceId,
            notification.TriggerId,
            notification.TriggerLogId,
            notification.ActionId,
            DeserializeMetadata(notification.MetadataJson),
            notification.ReadAt,
            notification.CreatedAt);
    }

    private static object? DeserializeMetadata(JsonDocument? metadata)
    {
        return metadata is null
            ? null
            : JsonSerializer.Deserialize<object>(metadata.RootElement.GetRawText(), JsonOptions);
    }
}
