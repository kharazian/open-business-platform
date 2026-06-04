using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class TriggerDefinition : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string EventName { get; set; } = string.Empty;

    public JsonDocument ConditionsJson { get; set; } = null!;

    public JsonDocument ActionsJson { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;

    public bool AutoRetryEnabled { get; set; } = true;

    public int AutoRetryMaxAttempts { get; set; } = 3;

    public int AutoRetryDelaySeconds { get; set; } = 60;

    public JsonDocument? ScheduleJson { get; set; }

    public DateTimeOffset? ScheduleNextRunAt { get; set; }

    public DateTimeOffset? ScheduleLastRunAt { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}
