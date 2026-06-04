using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerAutomaticRetryService
{
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerExecutionService triggerExecution;
    private readonly ILogger<TriggerAutomaticRetryService> logger;

    public TriggerAutomaticRetryService(
        OpenBusinessPlatformDbContext dbContext,
        TriggerExecutionService triggerExecution,
        ILogger<TriggerAutomaticRetryService> logger)
    {
        this.dbContext = dbContext;
        this.triggerExecution = triggerExecution;
        this.logger = logger;
    }

    public async Task<int> ProcessDueRetriesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dueLogs = await dbContext.TriggerLogs
            .Include(log => log.Trigger)
            .Where(log =>
                log.Status == TriggerExecutionStatuses.Failed
                && log.AutoRetryNextAttemptAt != null
                && log.AutoRetryNextAttemptAt <= now
                && log.AutoRetryLockedAt == null
                && log.AutoRetryCompletedAt == null
                && log.AutoRetryExhaustedAt == null
                && log.AutoRetryDisabledAt == null)
            .OrderBy(log => log.AutoRetryNextAttemptAt)
            .Take(10)
            .ToArrayAsync(cancellationToken);

        var processedCount = 0;

        foreach (var sourceLog in dueLogs)
        {
            var claimed = await TryClaimRetryAsync(sourceLog.Id, now, cancellationToken);
            if (!claimed)
            {
                continue;
            }

            TriggerRetryScheduler.MarkAttemptStarted(sourceLog, now);

            if (sourceLog.Trigger is null || !sourceLog.Trigger.IsEnabled)
            {
                TriggerRetryScheduler.MarkDisabled(sourceLog, now);
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            var activeRetryPolicy = TriggerRetryPolicy.FromTrigger(sourceLog.Trigger);

            if (activeRetryPolicy is null)
            {
                TriggerRetryScheduler.MarkDisabled(sourceLog, now);
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                var retriedLog = await triggerExecution.ExecuteAutomaticRetryAsync(sourceLog.Trigger, sourceLog, cancellationToken);
                var completedAt = DateTimeOffset.UtcNow;

                if (string.Equals(retriedLog.Status, TriggerExecutionStatuses.Success, StringComparison.Ordinal))
                {
                    TriggerRetryScheduler.MarkAttemptSucceeded(sourceLog, completedAt);
                }
                else
                {
                    TriggerRetryScheduler.MarkAttemptFailed(sourceLog, activeRetryPolicy, completedAt);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Automatic trigger retry failed before a retry log could be written for trigger log {TriggerLogId}.", sourceLog.Id);
                var retryPolicy = sourceLog.Trigger is null
                    ? TriggerRetryPolicy.Default
                    : TriggerRetryPolicy.FromTrigger(sourceLog.Trigger) ?? TriggerRetryPolicy.Default;
                TriggerRetryScheduler.MarkAttemptFailed(sourceLog, retryPolicy, DateTimeOffset.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            processedCount += 1;
        }

        return processedCount;
    }

    private async Task<bool> TryClaimRetryAsync(Guid sourceLogId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var claimedCount = await dbContext.TriggerLogs
            .Where(log =>
                log.Id == sourceLogId
                && log.Status == TriggerExecutionStatuses.Failed
                && log.AutoRetryNextAttemptAt != null
                && log.AutoRetryNextAttemptAt <= now
                && log.AutoRetryLockedAt == null
                && log.AutoRetryCompletedAt == null
                && log.AutoRetryExhaustedAt == null
                && log.AutoRetryDisabledAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(log => log.AutoRetryLockedAt, (DateTimeOffset?)now),
                cancellationToken);

        return claimedCount == 1;
    }
}
