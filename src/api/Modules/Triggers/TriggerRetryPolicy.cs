using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed record TriggerRetryPolicy(int MaxAttempts, TimeSpan Delay)
{
    public static TriggerRetryPolicy Default { get; } = new(3, TimeSpan.FromMinutes(1));

    public static TriggerRetryPolicy? FromDefinition(TriggerRetryPolicyDefinition? definition)
    {
        if (definition is null)
        {
            return Default;
        }

        return definition.IsEnabled && definition.MaxAttempts > 0
            ? new TriggerRetryPolicy(definition.MaxAttempts, TimeSpan.FromSeconds(definition.DelaySeconds))
            : null;
    }

    public static TriggerRetryPolicy? FromTrigger(TriggerDefinition trigger)
    {
        return FromDefinition(new TriggerRetryPolicyDefinition(
            trigger.AutoRetryEnabled,
            trigger.AutoRetryMaxAttempts,
            trigger.AutoRetryDelaySeconds));
    }
}

public static class TriggerRetryStates
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Exhausted = "exhausted";
    public const string Disabled = "disabled";
}

public static class TriggerRetryStateResolver
{
    public static string? Resolve(TriggerExecutionLog log, bool triggerEnabled)
    {
        if (!string.Equals(log.Status, TriggerExecutionStatuses.Failed, StringComparison.Ordinal))
        {
            return null;
        }

        if (log.AutoRetryCompletedAt is not null)
        {
            return TriggerRetryStates.Completed;
        }

        if (!triggerEnabled || log.AutoRetryDisabledAt is not null)
        {
            return TriggerRetryStates.Disabled;
        }

        if (log.AutoRetryExhaustedAt is not null
            || (log.AutoRetryMaxAttempts > 0 && log.AutoRetryAttemptCount >= log.AutoRetryMaxAttempts))
        {
            return TriggerRetryStates.Exhausted;
        }

        return log.AutoRetryNextAttemptAt is null ? null : TriggerRetryStates.Pending;
    }
}

public static class TriggerRetryScheduler
{
    public static void ScheduleInitialFailure(
        TriggerExecutionLog log,
        TriggerRetryPolicy policy,
        DateTimeOffset now)
    {
        log.AutoRetryAttemptCount = 0;
        log.AutoRetryMaxAttempts = policy.MaxAttempts;
        log.AutoRetryNextAttemptAt = now.Add(policy.Delay);
        log.AutoRetryLockedAt = null;
        log.AutoRetryCompletedAt = null;
        log.AutoRetryExhaustedAt = null;
        log.AutoRetryDisabledAt = null;
    }

    public static void MarkAttemptStarted(TriggerExecutionLog log, DateTimeOffset now)
    {
        log.AutoRetryLockedAt = now;
    }

    public static void MarkAttemptSucceeded(TriggerExecutionLog log, DateTimeOffset now)
    {
        log.AutoRetryLockedAt = null;
        log.AutoRetryNextAttemptAt = null;
        log.AutoRetryCompletedAt = now;
    }

    public static void MarkAttemptFailed(
        TriggerExecutionLog log,
        TriggerRetryPolicy policy,
        DateTimeOffset now)
    {
        log.AutoRetryLockedAt = null;
        log.AutoRetryAttemptCount += 1;
        log.AutoRetryMaxAttempts = policy.MaxAttempts;

        if (log.AutoRetryAttemptCount >= policy.MaxAttempts)
        {
            log.AutoRetryNextAttemptAt = null;
            log.AutoRetryExhaustedAt = now;
            return;
        }

        log.AutoRetryNextAttemptAt = now.Add(policy.Delay);
    }

    public static void MarkDisabled(TriggerExecutionLog log, DateTimeOffset now)
    {
        log.AutoRetryLockedAt = null;
        log.AutoRetryNextAttemptAt = null;
        log.AutoRetryDisabledAt = now;
    }
}
