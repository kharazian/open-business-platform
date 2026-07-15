using Microsoft.Extensions.Options;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class AutomationOutboxMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AutomationOutboxMonitorWorker> logger;
    private readonly AutomationHealthOptions options;
    private readonly HashSet<Guid> reportedDeadLetterIds = new();
    private Guid? reportedDelayedMessageId;

    public AutomationOutboxMonitorWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationOutboxMonitorWorker> logger,
        IOptions<AutomationHealthOptions> options)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.options = options.Value.Normalize();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.MonitorIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            await MonitorOnceAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task MonitorOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var snapshots = scope.ServiceProvider.GetRequiredService<AutomationOutboxSnapshotService>();
            var snapshot = await snapshots.GetSnapshotAsync(cancellationToken);

            foreach (var message in snapshot.DeadLetters.Where(message => reportedDeadLetterIds.Add(message.MessageId)))
            {
                logger.LogWarning(
                    "Trigger event delivery reached dead letter. FormId {FormId}, MessageId {MessageId}, EventName {EventName}, AttemptCount {AttemptCount}.",
                    message.FormId,
                    message.MessageId,
                    message.EventName,
                    message.AttemptCount);
            }

            if (snapshot.OldestPendingAgeSeconds >= options.PendingAgeWarningSeconds
                && snapshot.OldestPendingMessageId is not null
                && snapshot.OldestPendingMessageId != reportedDelayedMessageId)
            {
                reportedDelayedMessageId = snapshot.OldestPendingMessageId;
                logger.LogWarning(
                    "Trigger event delivery is delayed. FormId {FormId}, MessageId {MessageId}, PendingAgeSeconds {PendingAgeSeconds}, AttemptCount {AttemptCount}.",
                    snapshot.OldestPendingFormId,
                    snapshot.OldestPendingMessageId,
                    snapshot.OldestPendingAgeSeconds,
                    snapshot.OldestPendingAttemptCount);
            }
            else if (snapshot.OldestPendingAgeSeconds < options.PendingAgeWarningSeconds)
            {
                reportedDelayedMessageId = null;
            }

            if (reportedDeadLetterIds.Count > 10000)
            {
                reportedDeadLetterIds.Clear();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Automation delivery monitoring failed.");
        }
    }
}
