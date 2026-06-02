using System.Security.Claims;

namespace OpenBusinessPlatform.Api.Modules.Notifications;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("", async (
            NotificationQueryService notifications,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);

            if (userId is null)
            {
                return Results.Ok(new { items = Array.Empty<NotificationDto>() });
            }

            var items = await notifications.ListForUserAsync(userId.Value, cancellationToken);
            return Results.Ok(new { items });
        });

        group.MapGet("/unread-count", async (
            NotificationQueryService notifications,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);

            if (userId is null)
            {
                return Results.Ok(new NotificationUnreadCountDto(0));
            }

            return Results.Ok(await notifications.GetUnreadCountAsync(userId.Value, cancellationToken));
        });

        group.MapPost("/{notificationId:guid}/read", async (
            Guid notificationId,
            NotificationQueryService notifications,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);

            if (userId is null)
            {
                return Results.NotFound(new NotificationErrorResponse("Notification was not found."));
            }

            var notification = await notifications.MarkReadAsync(userId.Value, notificationId, cancellationToken);

            if (notification is null)
            {
                return Results.NotFound(new NotificationErrorResponse("Notification was not found."));
            }

            return Results.Ok(notification);
        });

        group.MapPost("/read-all", async (
            NotificationQueryService notifications,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);

            if (userId is null)
            {
                return Results.Ok(new NotificationUnreadCountDto(0));
            }

            return Results.Ok(await notifications.MarkAllReadAsync(userId.Value, cancellationToken));
        });

        return endpoints;
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
