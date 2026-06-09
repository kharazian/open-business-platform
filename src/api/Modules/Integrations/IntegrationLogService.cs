using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IntegrationLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenBusinessPlatformDbContext dbContext;

    public IntegrationLogService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<IntegrationLogDto>> ListAsync(CancellationToken cancellationToken)
    {
        var logs = await dbContext.IntegrationLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return logs.Select(ToDto).ToArray();
    }

    public async Task<IntegrationLogDto?> GetAsync(Guid logId, CancellationToken cancellationToken)
    {
        var log = await dbContext.IntegrationLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == logId, cancellationToken);

        return log is null ? null : ToDto(log);
    }

    public async Task<IntegrationLogDto> RecordAsync(
        RecordIntegrationLogRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var log = new IntegrationLogEntry
        {
            Id = Guid.NewGuid(),
            Direction = NormalizeSupported(request.Direction, IntegrationLogDirections.Supported, "Integration log direction"),
            IntegrationType = NormalizeSupported(request.IntegrationType, IntegrationLogTypes.Supported, "Integration type"),
            IntegrationKey = NormalizeIntegrationKey(request.IntegrationKey),
            SourceType = NormalizeSourceType(request.SourceType),
            SourceId = request.SourceId,
            TargetEntityType = NormalizeOptionalText(request.TargetEntityType, 80),
            TargetEntityId = request.TargetEntityId,
            Status = NormalizeSupported(request.Status, IntegrationLogStatuses.Supported, "Integration log status"),
            AttemptCount = Math.Max(0, request.AttemptCount),
            MaxAttempts = Math.Max(0, request.MaxAttempts),
            IsRetryable = request.IsRetryable,
            RetryNextAttemptAt = request.RetryNextAttemptAt,
            RequestMetadataJson = SerializeOptional(IntegrationMetadataSanitizer.Sanitize(request.RequestMetadata)),
            ResponseMetadataJson = SerializeOptional(IntegrationMetadataSanitizer.Sanitize(request.ResponseMetadata)),
            ErrorCode = NormalizeOptionalText(request.ErrorCode, 120),
            ErrorMessage = NormalizeOptionalText(request.ErrorMessage, 2000),
            StartedAt = request.StartedAt ?? now,
            CompletedAt = request.CompletedAt,
            CreatedById = createdById
        };

        dbContext.IntegrationLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(log);
    }

    public async Task<IntegrationLogDto?> RequestRetryAsync(
        Guid logId,
        RequestIntegrationRetryRequest request,
        Guid? requestedById,
        CancellationToken cancellationToken)
    {
        var log = await dbContext.IntegrationLogs
            .SingleOrDefaultAsync(candidate => candidate.Id == logId, cancellationToken);

        if (log is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(log.ConcurrencyStamp, request.ConcurrencyStamp);

        if (!log.IsRetryable || !string.Equals(log.Status, IntegrationLogStatuses.Failed, StringComparison.Ordinal))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "Only retryable failed integration logs can be marked for retry.");
        }

        var now = DateTimeOffset.UtcNow;
        log.RetryRequestedAt = now;
        log.RetryRequestedById = requestedById;
        log.RetryNextAttemptAt = request.RetryNextAttemptAt ?? now;
        log.RetryLockedAt = null;
        log.UpdatedById = requestedById;

        AddAudit(log.Id, "integration_log_retry_requested", requestedById, new
        {
            log.IntegrationKey,
            log.Direction,
            log.IntegrationType,
            log.SourceType,
            log.SourceId,
            log.TargetEntityType,
            log.TargetEntityId,
            log.RetryNextAttemptAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(log);
    }

    private static IntegrationLogDto ToDto(IntegrationLogEntry log)
    {
        return new IntegrationLogDto(
            log.Id,
            log.Direction,
            log.IntegrationType,
            log.IntegrationKey,
            log.SourceType,
            log.SourceId,
            log.TargetEntityType,
            log.TargetEntityId,
            log.Status,
            log.AttemptCount,
            log.MaxAttempts,
            log.IsRetryable,
            log.RetryNextAttemptAt,
            log.RetryLockedAt,
            log.RetryCompletedAt,
            log.RetryExhaustedAt,
            log.RetryRequestedAt,
            log.RetryRequestedById,
            DeserializeOptional(log.RequestMetadataJson),
            DeserializeOptional(log.ResponseMetadataJson),
            log.ErrorCode,
            log.ErrorMessage,
            log.StartedAt,
            log.CompletedAt,
            log.ConcurrencyStamp,
            log.CreatedAt,
            log.CreatedById,
            log.UpdatedAt,
            log.UpdatedById);
    }

    private static string NormalizeSupported(string? value, IReadOnlySet<string> supported, string label)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized) || !supported.Contains(normalized))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, $"{label} is invalid.");
        }

        return normalized;
    }

    private static string NormalizeIntegrationKey(string? value)
    {
        var normalized = NormalizeOptionalText(value, 120)?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "Integration key is required.");
        }

        return normalized;
    }

    private static string NormalizeSourceType(string? value)
    {
        var normalized = NormalizeOptionalText(value, 80);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "Integration log source type is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static JsonDocument? SerializeOptional(IReadOnlyDictionary<string, object?>? metadata)
    {
        return metadata is null ? null : JsonSerializer.SerializeToDocument(metadata, JsonOptions);
    }

    private static IReadOnlyDictionary<string, object?>? DeserializeOptional(JsonDocument? jsonDocument)
    {
        return jsonDocument?.RootElement.Deserialize<Dictionary<string, object?>>(JsonOptions);
    }

    private static void EnsureConcurrencyStamp(string current, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested) && !string.Equals(current, requested, StringComparison.Ordinal))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status409Conflict, "Integration log was updated by someone else. Refresh and try again.");
        }
    }

    private void AddAudit(Guid entityId, string action, Guid? userId, object? metadata = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "IntegrationLog",
            EntityId = entityId,
            Action = action,
            UserId = userId,
            MetadataJson = metadata is null ? null : JsonSerializer.SerializeToDocument(metadata, JsonOptions)
        });
    }
}
