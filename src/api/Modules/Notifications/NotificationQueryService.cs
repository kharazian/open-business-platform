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

    public async Task<NotificationPageDto> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);
        var total = await query.LongCountAsync(cancellationToken);
        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new NotificationPageDto(notifications.Select(ToDto).ToArray(), page, pageSize, total);
    }

    public async Task<NotificationUnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && notification.ReadAt == null, cancellationToken);

        return new NotificationUnreadCountDto(unreadCount);
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        return preference is null ? DefaultPreferences() : ToDto(preference);
    }

    public async Task<NotificationPreferencesDto?> UpdatePreferencesAsync(
        Guid userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var preference = await dbContext.NotificationPreferences
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (preference is null)
        {
            preference = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            dbContext.NotificationPreferences.Add(preference);
        }

        preference.InAppEnabled = request.InAppEnabled;
        preference.ShowUnreadBadge = request.ShowUnreadBadge;
        preference.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(preference);
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
        await dbContext.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(notification => notification.ReadAt, DateTimeOffset.UtcNow), cancellationToken);

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

    private static NotificationPreferencesDto DefaultPreferences()
    {
        return new NotificationPreferencesDto(true, true, null);
    }

    private static NotificationPreferencesDto ToDto(NotificationPreference preference)
    {
        return new NotificationPreferencesDto(preference.InAppEnabled, preference.ShowUnreadBadge, preference.UpdatedAt);
    }

    private static object? DeserializeMetadata(JsonDocument? metadata)
    {
        return metadata is null
            ? null
            : JsonSerializer.Deserialize<object>(metadata.RootElement.GetRawText(), JsonOptions);
    }
}
