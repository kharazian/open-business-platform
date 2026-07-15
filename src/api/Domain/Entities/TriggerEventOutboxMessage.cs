using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class TriggerEventOutboxMessage : Entity<Guid>, IHasCreationTime
{
    public Guid FormId { get; set; }

    public Guid RecordId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public JsonDocument PayloadJson { get; set; } = null!;

    public string Status { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public DateTimeOffset NextAttemptAt { get; set; }

    public DateTimeOffset? LockedAt { get; set; }

    public Guid? ClaimId { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? DeadLetteredAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
