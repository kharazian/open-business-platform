namespace OpenBusinessPlatform.Api.Modules.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string SourceType,
    Guid? SourceId,
    Guid? TriggerId,
    Guid? TriggerLogId,
    string? ActionId,
    object? Metadata,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

public sealed record NotificationUnreadCountDto(int UnreadCount);

public sealed record NotificationPreferencesDto(bool InAppEnabled, bool ShowUnreadBadge, DateTimeOffset? UpdatedAt);

public sealed record UpdateNotificationPreferencesRequest(bool InAppEnabled, bool ShowUnreadBadge);

public sealed record NotificationErrorResponse(string Message);
