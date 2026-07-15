using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerEventOutboxProcessor
{
    public static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int BatchSize = 25;
    private const int MaxErrorLength = 2000;
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerEventDispatcher dispatcher;
    private readonly ILogger<TriggerEventOutboxProcessor> logger;

    public TriggerEventOutboxProcessor(
        OpenBusinessPlatformDbContext dbContext,
        TriggerEventDispatcher dispatcher,
        ILogger<TriggerEventOutboxProcessor> logger)
    {
        this.dbContext = dbContext;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredBefore = now - ClaimLease;
        var candidateIds = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message =>
                (message.Status == TriggerEventOutboxStatuses.Pending && message.NextAttemptAt <= now)
                || (message.Status == TriggerEventOutboxStatuses.Processing && message.LockedAt < expiredBefore))
            .OrderBy(message => message.NextAttemptAt)
            .ThenBy(message => message.CreatedAt)
            .Select(message => message.Id)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken);

        var processed = 0;
        foreach (var messageId in candidateIds)
        {
            var claimId = Guid.NewGuid();
            var claimedAt = DateTimeOffset.UtcNow;
            var claimExpiredBefore = claimedAt - ClaimLease;
            var claimed = await dbContext.TriggerEventOutbox
                .Where(message => message.Id == messageId)
                .Where(message =>
                    (message.Status == TriggerEventOutboxStatuses.Pending && message.NextAttemptAt <= claimedAt)
                    || (message.Status == TriggerEventOutboxStatuses.Processing && message.LockedAt < claimExpiredBefore))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Status, TriggerEventOutboxStatuses.Processing)
                    .SetProperty(message => message.LockedAt, claimedAt)
                    .SetProperty(message => message.ClaimId, claimId)
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                    .SetProperty(message => message.ErrorMessage, (string?)null), cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            processed++;
            await DeliverClaimAsync(messageId, claimId, cancellationToken);
        }

        return processed;
    }

    private async Task DeliverClaimAsync(Guid messageId, Guid claimId, CancellationToken cancellationToken)
    {
        var message = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .FirstAsync(candidate => candidate.Id == messageId && candidate.ClaimId == claimId, cancellationToken);

        try
        {
            var context = JsonSerializer.Deserialize<TriggerEventContext>(message.PayloadJson.RootElement.GetRawText(), JsonOptions)
                ?? throw new InvalidOperationException("Trigger event outbox payload is empty.");
            await dispatcher.DispatchAsync(context, cancellationToken);

            await dbContext.TriggerEventOutbox
                .Where(candidate => candidate.Id == messageId && candidate.ClaimId == claimId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, TriggerEventOutboxStatuses.Completed)
                    .SetProperty(candidate => candidate.CompletedAt, DateTimeOffset.UtcNow)
                    .SetProperty(candidate => candidate.LockedAt, (DateTimeOffset?)null)
                    .SetProperty(candidate => candidate.ClaimId, (Guid?)null)
                    .SetProperty(candidate => candidate.ErrorMessage, (string?)null), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Trigger event outbox message {MessageId} delivery failed.", messageId);
            var exhausted = message.AttemptCount >= message.MaxAttempts;
            var failedAt = DateTimeOffset.UtcNow;
            var nextAttemptAt = failedAt + CalculateRetryDelay(message.AttemptCount);
            var errorMessage = exception.Message.Length <= MaxErrorLength
                ? exception.Message
                : exception.Message[..MaxErrorLength];

            await dbContext.TriggerEventOutbox
                .Where(candidate => candidate.Id == messageId && candidate.ClaimId == claimId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, exhausted ? TriggerEventOutboxStatuses.DeadLetter : TriggerEventOutboxStatuses.Pending)
                    .SetProperty(candidate => candidate.NextAttemptAt, nextAttemptAt)
                    .SetProperty(candidate => candidate.DeadLetteredAt, exhausted ? failedAt : null)
                    .SetProperty(candidate => candidate.LockedAt, (DateTimeOffset?)null)
                    .SetProperty(candidate => candidate.ClaimId, (Guid?)null)
                    .SetProperty(candidate => candidate.ErrorMessage, errorMessage), cancellationToken);
        }
    }

    public static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var exponent = Math.Clamp(attemptCount - 1, 0, 5);
        return TimeSpan.FromSeconds(Math.Min(30 * Math.Pow(2, exponent), 900));
    }
}
