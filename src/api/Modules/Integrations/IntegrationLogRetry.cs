using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed record IntegrationRetryPolicy(int MaxAttempts, TimeSpan Delay)
{
    public static IntegrationRetryPolicy Default { get; } = new(3, TimeSpan.FromMinutes(1));
}

public static class IntegrationRetryStates
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Exhausted = "exhausted";
}

public static class IntegrationRetryStateResolver
{
    public static string? Resolve(IntegrationLogEntry log)
    {
        if (!log.IsRetryable || !string.Equals(log.Status, IntegrationLogStatuses.Failed, StringComparison.Ordinal))
        {
            return null;
        }

        if (log.RetryCompletedAt is not null)
        {
            return IntegrationRetryStates.Completed;
        }

        if (log.RetryExhaustedAt is not null
            || (log.MaxAttempts > 0 && log.AttemptCount >= log.MaxAttempts))
        {
            return IntegrationRetryStates.Exhausted;
        }

        return log.RetryNextAttemptAt is null ? null : IntegrationRetryStates.Pending;
    }

    public static string? Resolve(IntegrationLogDto log)
    {
        if (!log.IsRetryable || !string.Equals(log.Status, IntegrationLogStatuses.Failed, StringComparison.Ordinal))
        {
            return null;
        }

        if (log.RetryCompletedAt is not null)
        {
            return IntegrationRetryStates.Completed;
        }

        if (log.RetryExhaustedAt is not null
            || (log.MaxAttempts > 0 && log.AttemptCount >= log.MaxAttempts))
        {
            return IntegrationRetryStates.Exhausted;
        }

        return log.RetryNextAttemptAt is null ? null : IntegrationRetryStates.Pending;
    }
}

public static class IntegrationRetryScheduler
{
    public static void ScheduleRetry(IntegrationLogEntry log, IntegrationRetryPolicy policy, DateTimeOffset now)
    {
        log.IsRetryable = true;
        log.AttemptCount = Math.Max(0, log.AttemptCount);
        log.MaxAttempts = policy.MaxAttempts;
        log.RetryNextAttemptAt = now.Add(policy.Delay);
        log.RetryLockedAt = null;
        log.RetryCompletedAt = null;
        log.RetryExhaustedAt = null;
    }

    public static void MarkAttemptFailed(IntegrationLogEntry log, IntegrationRetryPolicy policy, DateTimeOffset now)
    {
        log.RetryLockedAt = null;
        log.AttemptCount += 1;
        log.MaxAttempts = policy.MaxAttempts;

        if (log.AttemptCount >= policy.MaxAttempts)
        {
            log.RetryNextAttemptAt = null;
            log.RetryExhaustedAt = now;
            return;
        }

        log.RetryNextAttemptAt = now.Add(policy.Delay);
    }
}
