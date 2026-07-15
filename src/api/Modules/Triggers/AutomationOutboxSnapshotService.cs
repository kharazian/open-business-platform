using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed record AutomationAttentionMessage(
    Guid MessageId,
    Guid FormId,
    string EventName,
    int AttemptCount,
    DateTimeOffset CreatedAt);

public sealed record AutomationOutboxSnapshot(
    string Status,
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int DeadLetterCount,
    int RetryBacklogCount,
    double OldestPendingAgeSeconds,
    Guid? OldestPendingMessageId,
    Guid? OldestPendingFormId,
    int OldestPendingAttemptCount,
    IReadOnlyCollection<AutomationAttentionMessage> DeadLetters,
    DateTimeOffset ObservedAt);

public sealed class AutomationOutboxSnapshotService
{
    private const int AttentionMessageLimit = 100;
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly AutomationHealthOptions options;

    public AutomationOutboxSnapshotService(
        OpenBusinessPlatformDbContext dbContext,
        IOptions<AutomationHealthOptions> options)
    {
        this.dbContext = dbContext;
        this.options = options.Value.Normalize();
    }

    public async Task<AutomationOutboxSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var observedAt = DateTimeOffset.UtcNow;
        var counts = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .GroupBy(message => message.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var retryBacklogCount = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .CountAsync(message => message.Status == TriggerEventOutboxStatuses.Pending && message.AttemptCount > 0, cancellationToken);
        var oldestPending = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message => message.Status == TriggerEventOutboxStatuses.Pending)
            .OrderBy(message => message.CreatedAt)
            .Select(message => new
            {
                message.Id,
                message.FormId,
                message.CreatedAt,
                message.AttemptCount
            })
            .FirstOrDefaultAsync(cancellationToken);
        var deadLetters = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message => message.Status == TriggerEventOutboxStatuses.DeadLetter)
            .OrderBy(message => message.DeadLetteredAt)
            .Select(message => new AutomationAttentionMessage(
                message.Id,
                message.FormId,
                message.EventName,
                message.AttemptCount,
                message.CreatedAt))
            .Take(AttentionMessageLimit)
            .ToArrayAsync(cancellationToken);
        var oldestPendingAgeSeconds = oldestPending is null
            ? 0
            : Math.Max(0, (observedAt - oldestPending.CreatedAt).TotalSeconds);
        var deadLetterCount = GetCount(counts, TriggerEventOutboxStatuses.DeadLetter);
        var status = GetStatus(deadLetterCount, oldestPendingAgeSeconds, options);

        return new AutomationOutboxSnapshot(
            status,
            GetCount(counts, TriggerEventOutboxStatuses.Pending),
            GetCount(counts, TriggerEventOutboxStatuses.Processing),
            GetCount(counts, TriggerEventOutboxStatuses.Completed),
            deadLetterCount,
            retryBacklogCount,
            oldestPendingAgeSeconds,
            oldestPending?.Id,
            oldestPending?.FormId,
            oldestPending?.AttemptCount ?? 0,
            deadLetters,
            observedAt);
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string status)
    {
        return counts.GetValueOrDefault(status);
    }

    public static string GetStatus(
        int deadLetterCount,
        double oldestPendingAgeSeconds,
        AutomationHealthOptions options)
    {
        var normalized = options.Normalize();
        return deadLetterCount >= normalized.DeadLetterWarningCount
            || oldestPendingAgeSeconds >= normalized.PendingAgeWarningSeconds
            ? "degraded"
            : "healthy";
    }
}
