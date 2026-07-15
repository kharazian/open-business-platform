namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerEventOutboxWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TriggerEventOutboxWorker> logger;

    public TriggerEventOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<TriggerEventOutboxWorker> logger)
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
            var processor = scope.ServiceProvider.GetRequiredService<TriggerEventOutboxProcessor>();
            await processor.ProcessDueAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Trigger event outbox processing failed.");
        }
    }
}
