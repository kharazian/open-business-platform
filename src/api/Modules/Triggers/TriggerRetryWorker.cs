namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerRetryWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TriggerRetryWorker> logger;

    public TriggerRetryWorker(IServiceScopeFactory scopeFactory, ILogger<TriggerRetryWorker> logger)
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
            var retryService = scope.ServiceProvider.GetRequiredService<TriggerAutomaticRetryService>();
            await retryService.ProcessDueRetriesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Automatic trigger retry processing failed.");
        }
    }
}
