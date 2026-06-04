using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class TriggerExecutionLog : Entity<Guid>, IHasCreationTime
{
    public Guid TriggerId { get; set; }

    public TriggerDefinition? Trigger { get; set; }

    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Status { get; set; } = string.Empty;

    public JsonDocument InputJson { get; set; } = null!;

    public JsonDocument? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public int AutoRetryAttemptCount { get; set; }

    public int AutoRetryMaxAttempts { get; set; } = 3;

    public DateTimeOffset? AutoRetryNextAttemptAt { get; set; }

    public DateTimeOffset? AutoRetryLockedAt { get; set; }

    public DateTimeOffset? AutoRetryCompletedAt { get; set; }

    public DateTimeOffset? AutoRetryExhaustedAt { get; set; }

    public DateTimeOffset? AutoRetryDisabledAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
