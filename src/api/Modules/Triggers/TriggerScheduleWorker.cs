namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerScheduleWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TriggerScheduleWorker> logger;

    public TriggerScheduleWorker(IServiceScopeFactory scopeFactory, ILogger<TriggerScheduleWorker> logger)
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
            var scheduleService = scope.ServiceProvider.GetRequiredService<TriggerScheduleService>();
            await scheduleService.ProcessDueSchedulesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scheduled trigger processing failed.");
        }
    }
}
