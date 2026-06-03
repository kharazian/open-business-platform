using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class NotificationPreference : Entity<Guid>
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public bool InAppEnabled { get; set; } = true;

    public bool ShowUnreadBadge { get; set; } = true;

    public DateTimeOffset UpdatedAt { get; set; }
}
