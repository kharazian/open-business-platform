using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerScheduleService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static TimeSpan ClaimLease { get; } = TimeSpan.FromMinutes(5);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerExecutionService triggerExecution;

    public TriggerScheduleService(
        OpenBusinessPlatformDbContext dbContext,
        TriggerExecutionService triggerExecution)
    {
        this.dbContext = dbContext;
        this.triggerExecution = triggerExecution;
    }

    public async Task<TriggerScheduledRunResultDto> RunScheduleNowAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var trigger = await dbContext.Triggers
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == triggerId && !candidate.IsDeleted, cancellationToken);

        if (trigger is null)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger was not found.");
        }

        if (!TriggerEvents.IsScheduled(trigger.EventName))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Only scheduled triggers can be run manually.");
        }

        if (!trigger.IsEnabled)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Disabled scheduled triggers cannot be run manually.");
        }

        var schedule = DeserializeSchedule(trigger.ScheduleJson);

        if (schedule is null)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Schedule metadata is not available for this trigger.");
        }

        var runAt = DateTimeOffset.UtcNow;
        if (!await TryClaimScheduleAsync(trigger.Id, runAt, requireDue: false, cancellationToken))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "This scheduled trigger is already running.");
        }

        trigger = await dbContext.Triggers
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == triggerId, cancellationToken);
        var nextRunAt = trigger.ScheduleNextRunAt is { } existingNextRunAt && existingNextRunAt > runAt
            ? existingNextRunAt
            : TriggerScheduleCalculator.CalculateNextRun(schedule, runAt);

        try
        {
            var log = await triggerExecution.ExecuteScheduledAsync(
                trigger,
                runAt,
                runAt,
                nextRunAt,
                TriggerScheduleRunSources.Manual,
                cancellationToken);
            var completedAt = log.CompletedAt ?? DateTimeOffset.UtcNow;
            await CompleteClaimAsync(trigger.Id, runAt, completedAt, nextRunAt, cancellationToken);

            return new TriggerScheduledRunResultDto(
                TriggerDefinitionService.ToLogDto(log),
                nextRunAt,
                completedAt);
        }
        catch
        {
            await ReleaseClaimAsync(trigger.Id, runAt, cancellationToken);
            throw;
        }
    }

    public async Task<int> ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dueTriggerIds = await dbContext.Triggers
            .AsNoTracking()
            .Where(trigger =>
                trigger.IsEnabled
                && !trigger.IsDeleted
                && trigger.ScheduleNextRunAt != null
                && trigger.ScheduleNextRunAt <= now
                && (trigger.ScheduleLockedAt == null || trigger.ScheduleLockedAt < now - ClaimLease))
            .OrderBy(trigger => trigger.ScheduleNextRunAt)
            .Select(trigger => trigger.Id)
            .Take(10)
            .ToArrayAsync(cancellationToken);
        var processedCount = 0;

        foreach (var triggerId in dueTriggerIds)
        {
            var lockedAt = DateTimeOffset.UtcNow;
            if (!await TryClaimScheduleAsync(triggerId, lockedAt, requireDue: true, cancellationToken))
            {
                continue;
            }

            var trigger = await dbContext.Triggers
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == triggerId, cancellationToken);
            var schedule = DeserializeSchedule(trigger.ScheduleJson);
            var dueAt = trigger.ScheduleNextRunAt ?? now;

            try
            {
                if (schedule is null)
                {
                    await triggerExecution.SkipScheduledAsync(
                        trigger,
                        dueAt,
                        lockedAt,
                        "schedule_metadata_unavailable",
                        cancellationToken);
                    await CompleteClaimAsync(trigger.Id, lockedAt, lockedAt, null, cancellationToken);
                    processedCount += 1;
                    continue;
                }

                var nextRunAt = TriggerScheduleCalculator.CalculateNextRun(schedule, lockedAt);
                var log = await triggerExecution.ExecuteScheduledAsync(trigger, dueAt, lockedAt, nextRunAt, cancellationToken);
                var completedAt = log.CompletedAt ?? DateTimeOffset.UtcNow;
                await CompleteClaimAsync(trigger.Id, lockedAt, completedAt, nextRunAt, cancellationToken);
                processedCount += 1;
            }
            catch
            {
                await ReleaseClaimAsync(trigger.Id, lockedAt, cancellationToken);
                throw;
            }
        }

        return processedCount;
    }

    private static TriggerScheduleDefinition? DeserializeSchedule(JsonDocument? scheduleJson)
    {
        var schedule = scheduleJson?.RootElement.Deserialize<TriggerScheduleDefinition>(JsonOptions);
        return TriggerDefinitionValidator.NormalizeSchedule(schedule);
    }

    private async Task<bool> TryClaimScheduleAsync(
        Guid triggerId,
        DateTimeOffset lockedAt,
        bool requireDue,
        CancellationToken cancellationToken)
    {
        var expiredBefore = lockedAt - ClaimLease;
        var candidates = dbContext.Triggers.Where(trigger =>
            trigger.Id == triggerId
            && trigger.IsEnabled
            && !trigger.IsDeleted
            && (trigger.ScheduleLockedAt == null || trigger.ScheduleLockedAt < expiredBefore));

        if (requireDue)
        {
            candidates = candidates.Where(trigger =>
                trigger.ScheduleNextRunAt != null && trigger.ScheduleNextRunAt <= lockedAt);
        }

        var claimed = await candidates.ExecuteUpdateAsync(
            updates => updates.SetProperty(trigger => trigger.ScheduleLockedAt, lockedAt),
            cancellationToken);
        return claimed == 1;
    }

    private async Task CompleteClaimAsync(
        Guid triggerId,
        DateTimeOffset lockedAt,
        DateTimeOffset completedAt,
        DateTimeOffset? nextRunAt,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.Triggers
            .Where(trigger => trigger.Id == triggerId && trigger.ScheduleLockedAt == lockedAt)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(trigger => trigger.ScheduleLastRunAt, completedAt)
                    .SetProperty(trigger => trigger.ScheduleNextRunAt, nextRunAt)
                    .SetProperty(trigger => trigger.ScheduleLockedAt, (DateTimeOffset?)null),
                cancellationToken);

        if (updated != 1)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "The scheduled trigger claim was lost before completion.");
        }
    }

    private async Task ReleaseClaimAsync(Guid triggerId, DateTimeOffset lockedAt, CancellationToken cancellationToken)
    {
        await dbContext.Triggers
            .Where(trigger => trigger.Id == triggerId && trigger.ScheduleLockedAt == lockedAt)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(trigger => trigger.ScheduleLockedAt, (DateTimeOffset?)null),
                cancellationToken);
    }
}
