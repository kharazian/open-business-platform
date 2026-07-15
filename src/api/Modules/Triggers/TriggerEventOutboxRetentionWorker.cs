namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerEventOutboxRetentionWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TriggerEventOutboxRetentionWorker> logger;

    public TriggerEventOutboxRetentionWorker(IServiceScopeFactory scopeFactory, ILogger<TriggerEventOutboxRetentionWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOnceAsync(stoppingToken);

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

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var retention = scope.ServiceProvider.GetRequiredService<TriggerEventOutboxRetentionService>();
            var deleted = await retention.DeleteExpiredCompletedAsync(cancellationToken);

            if (deleted > 0)
            {
                logger.LogInformation("Deleted {MessageCount} expired completed trigger event outbox messages.", deleted);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Trigger event outbox retention failed.");
        }
    }
}
