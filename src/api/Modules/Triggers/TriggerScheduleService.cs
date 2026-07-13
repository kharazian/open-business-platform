using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerScheduleService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
        var nextRunAt = TriggerScheduleCalculator.CalculateNextRun(schedule, runAt);
        var log = await triggerExecution.ExecuteScheduledAsync(
            trigger,
            runAt,
            runAt,
            nextRunAt,
            TriggerScheduleRunSources.Manual,
            cancellationToken);
        var completedAt = log.CompletedAt ?? DateTimeOffset.UtcNow;

        trigger.ScheduleLastRunAt = completedAt;
        trigger.ScheduleNextRunAt = nextRunAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TriggerScheduledRunResultDto(
            TriggerDefinitionService.ToLogDto(log),
            trigger.ScheduleNextRunAt,
            trigger.ScheduleLastRunAt);
    }

    public async Task<int> ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dueTriggers = await dbContext.Triggers
            .Where(trigger =>
                trigger.IsEnabled
                && !trigger.IsDeleted
                && trigger.ScheduleNextRunAt != null
                && trigger.ScheduleNextRunAt <= now)
            .OrderBy(trigger => trigger.ScheduleNextRunAt)
            .Take(10)
            .ToArrayAsync(cancellationToken);
        var processedCount = 0;

        foreach (var trigger in dueTriggers)
        {
            var schedule = DeserializeSchedule(trigger.ScheduleJson);
            var dueAt = trigger.ScheduleNextRunAt ?? now;
            var lockedAt = DateTimeOffset.UtcNow;

            if (schedule is null)
            {
                await triggerExecution.SkipScheduledAsync(
                    trigger,
                    dueAt,
                    lockedAt,
                    "schedule_metadata_unavailable",
                    cancellationToken);
                trigger.ScheduleLastRunAt = lockedAt;
                trigger.ScheduleNextRunAt = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                processedCount += 1;
                continue;
            }

            var nextRunAt = TriggerScheduleCalculator.CalculateNextRun(schedule, lockedAt);
            var log = await triggerExecution.ExecuteScheduledAsync(trigger, dueAt, lockedAt, nextRunAt, cancellationToken);
            var completedAt = log.CompletedAt ?? DateTimeOffset.UtcNow;

            trigger.ScheduleLastRunAt = completedAt;
            trigger.ScheduleNextRunAt = nextRunAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            processedCount += 1;
        }

        return processedCount;
    }

    private static TriggerScheduleDefinition? DeserializeSchedule(JsonDocument? scheduleJson)
    {
        var schedule = scheduleJson?.RootElement.Deserialize<TriggerScheduleDefinition>(JsonOptions);
        return TriggerDefinitionValidator.NormalizeSchedule(schedule);
    }
}
