using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed record TriggerEventOutboxSummaryDto(
    Guid FormId,
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int DeadLetterCount,
    DateTimeOffset? OldestPendingAt,
    string HealthStatus);

public sealed record TriggerEventOutboxMessageDto(
    Guid Id,
    Guid FormId,
    Guid RecordId,
    string EventName,
    string Status,
    int AttemptCount,
    int MaxAttempts,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? LockedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DeadLetteredAt,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);

public sealed record TriggerEventOutboxOperationsDto(
    TriggerEventOutboxSummaryDto Summary,
    IReadOnlyCollection<TriggerEventOutboxMessageDto> Items);

public sealed class TriggerEventOutboxOperationsService
{
    private const int ListLimit = 100;
    private static readonly TimeSpan DelayedThreshold = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Expression<Func<TriggerEventOutboxMessage, TriggerEventOutboxMessageDto>> ToDtoProjection = message =>
        new TriggerEventOutboxMessageDto(
            message.Id,
            message.FormId,
            message.RecordId,
            message.EventName,
            message.Status,
            message.AttemptCount,
            message.MaxAttempts,
            message.NextAttemptAt,
            message.LockedAt,
            message.CompletedAt,
            message.DeadLetteredAt,
            message.ErrorMessage,
            message.CreatedAt);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public TriggerEventOutboxOperationsService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<TriggerEventOutboxOperationsDto> GetOperationsAsync(
        Guid formId,
        string? status,
        CancellationToken cancellationToken)
    {
        var normalizedStatus = NormalizeStatus(status);
        var counts = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message => message.FormId == formId)
            .GroupBy(message => message.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var oldestPendingAt = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message => message.FormId == formId && message.Status == TriggerEventOutboxStatuses.Pending)
            .MinAsync(message => (DateTimeOffset?)message.CreatedAt, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var summary = new TriggerEventOutboxSummaryDto(
            formId,
            GetCount(counts, TriggerEventOutboxStatuses.Pending),
            GetCount(counts, TriggerEventOutboxStatuses.Processing),
            GetCount(counts, TriggerEventOutboxStatuses.Completed),
            GetCount(counts, TriggerEventOutboxStatuses.DeadLetter),
            oldestPendingAt,
            GetHealthStatus(GetCount(counts, TriggerEventOutboxStatuses.DeadLetter), oldestPendingAt, now));

        var items = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message => message.FormId == formId && message.Status == normalizedStatus)
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Take(ListLimit)
            .Select(ToDtoProjection)
            .ToArrayAsync(cancellationToken);

        return new TriggerEventOutboxOperationsDto(summary, items);
    }

    public async Task<TriggerEventOutboxMessageDto> ReplayDeadLetterAsync(
        Guid formId,
        Guid messageId,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var exists = await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .AnyAsync(message => message.Id == messageId && message.FormId == formId, cancellationToken);

        if (!exists)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger event outbox message was not found.");
        }

        var replayedAt = DateTimeOffset.UtcNow;
        var updated = await dbContext.TriggerEventOutbox
            .Where(message =>
                message.Id == messageId
                && message.FormId == formId
                && message.Status == TriggerEventOutboxStatuses.DeadLetter)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, TriggerEventOutboxStatuses.Pending)
                .SetProperty(message => message.AttemptCount, 0)
                .SetProperty(message => message.NextAttemptAt, replayedAt)
                .SetProperty(message => message.LockedAt, (DateTimeOffset?)null)
                .SetProperty(message => message.ClaimId, (Guid?)null)
                .SetProperty(message => message.CompletedAt, (DateTimeOffset?)null)
                .SetProperty(message => message.DeadLetteredAt, (DateTimeOffset?)null)
                .SetProperty(message => message.ErrorMessage, (string?)null), cancellationToken);

        if (updated == 0)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Only a dead-letter trigger event can be replayed.");
        }

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "TriggerEventOutbox",
            EntityId = messageId,
            Action = "trigger_event_outbox_replayed",
            UserId = actorUserId,
            MetadataJson = JsonSerializer.SerializeToDocument(new { formId, replayedAt }, JsonOptions)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await dbContext.TriggerEventOutbox
            .AsNoTracking()
            .Where(message => message.Id == messageId)
            .Select(ToDtoProjection)
            .SingleAsync(cancellationToken);
    }

    public static string GetHealthStatus(int deadLetterCount, DateTimeOffset? oldestPendingAt, DateTimeOffset now)
    {
        if (deadLetterCount > 0)
        {
            return "attention";
        }

        return oldestPendingAt is not null && now - oldestPendingAt.Value > DelayedThreshold
            ? "delayed"
            : "healthy";
    }

    private static string NormalizeStatus(string? status)
    {
        var normalized = string.IsNullOrWhiteSpace(status)
            ? TriggerEventOutboxStatuses.DeadLetter
            : status.Trim().ToLowerInvariant();

        if (!TriggerEventOutboxStatuses.Supported.Contains(normalized))
        {
            throw new TriggerManagementException(StatusCodes.Status400BadRequest, "Trigger event outbox status is invalid.");
        }

        return normalized;
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string status)
    {
        return counts.GetValueOrDefault(status);
    }

}
