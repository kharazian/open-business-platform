using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerEventOutboxRetentionService
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);
    public const int BatchSize = 500;
    private readonly OpenBusinessPlatformDbContext dbContext;

    public TriggerEventOutboxRetentionService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<int> DeleteExpiredCompletedAsync(CancellationToken cancellationToken)
    {
        var expiredBefore = DateTimeOffset.UtcNow - RetentionPeriod;
        var ids = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message =>
                message.Status == TriggerEventOutboxStatuses.Completed
                && message.CompletedAt < expiredBefore)
            .OrderBy(message => message.CompletedAt)
            .Select(message => message.Id)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken);

        if (ids.Length == 0)
        {
            return 0;
        }

        return await dbContext.TriggerEventOutbox
            .Where(message => ids.Contains(message.Id) && message.Status == TriggerEventOutboxStatuses.Completed)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
