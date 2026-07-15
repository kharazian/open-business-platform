using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class AutomationOutboxHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory scopeFactory;

    public AutomationOutboxHealthCheck(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var snapshots = scope.ServiceProvider.GetRequiredService<AutomationOutboxSnapshotService>();
            var snapshot = await snapshots.GetSnapshotAsync(cancellationToken);
            var data = new Dictionary<string, object>
            {
                ["pendingCount"] = snapshot.PendingCount,
                ["processingCount"] = snapshot.ProcessingCount,
                ["deadLetterCount"] = snapshot.DeadLetterCount,
                ["retryBacklogCount"] = snapshot.RetryBacklogCount,
                ["oldestPendingAgeSeconds"] = snapshot.OldestPendingAgeSeconds
            };

            return snapshot.Status == "degraded"
                ? HealthCheckResult.Degraded("Automation delivery requires attention.", data: data)
                : HealthCheckResult.Healthy("Automation delivery is healthy.", data);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Automation delivery health query failed.", exception);
        }
    }
}
