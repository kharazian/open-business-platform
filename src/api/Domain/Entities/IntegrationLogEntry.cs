using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class IntegrationLogEntry : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Direction { get; set; } = string.Empty;

    public string IntegrationType { get; set; } = string.Empty;

    public string IntegrationKey { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public Guid? SourceId { get; set; }

    public string? TargetEntityType { get; set; }

    public Guid? TargetEntityId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public bool IsRetryable { get; set; }

    public DateTimeOffset? RetryNextAttemptAt { get; set; }

    public DateTimeOffset? RetryLockedAt { get; set; }

    public DateTimeOffset? RetryCompletedAt { get; set; }

    public DateTimeOffset? RetryExhaustedAt { get; set; }

    public DateTimeOffset? RetryRequestedAt { get; set; }

    public Guid? RetryRequestedById { get; set; }

    public User? RetryRequestedBy { get; set; }

    public JsonDocument? RequestMetadataJson { get; set; }

    public JsonDocument? ResponseMetadataJson { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}
