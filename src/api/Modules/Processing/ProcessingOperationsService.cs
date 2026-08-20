using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public sealed class ProcessingOperationsService(
    OpenBusinessPlatformDbContext dbContext,
    PermissionService permissions,
    ILogger<ProcessingOperationsService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProcessingJobPageDto<ProcessingOperationalLogDto>> ListAsync(
        int page,
        int pageSize,
        Guid? definitionId,
        Guid? runId,
        string? kind,
        string? severity,
        string? eventCode,
        string? errorCode,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        (var rangeFrom, var rangeTo) = Range(from, to);
        (page, pageSize) = Page(page, pageSize);
        kind = Normalize(kind);
        severity = Normalize(severity);
        eventCode = Normalize(eventCode);
        errorCode = Normalize(errorCode);
        if (kind is not null && !ProcessingJobKinds.Supported.Contains(kind)) throw InvalidFilter("kind");
        if (severity is not null && !ProcessingOperationalLogSeverities.Supported.Contains(severity)) throw InvalidFilter("severity");
        if (eventCode is not null && !ProcessingOperationalEventCodes.Supported.Contains(eventCode)) throw InvalidFilter("eventCode");
        if (definitionId is { } definition && !await dbContext.ProcessingJobDefinitions.AsNoTracking().AnyAsync(x => x.Id == definition && !x.IsDeleted, ct))
            throw new ProcessingJobException(StatusCodes.Status404NotFound, "Processing job was not found.");
        if (runId is { } run && !await dbContext.ProcessingJobRuns.AsNoTracking().AnyAsync(x => x.Id == run && (definitionId == null || x.DefinitionId == definitionId), ct))
            throw new ProcessingJobException(StatusCodes.Status404NotFound, "Processing run was not found.");

        var query = dbContext.ProcessingOperationalLogs.AsNoTracking().Where(x => x.OccurredAt >= rangeFrom && x.OccurredAt <= rangeTo);
        if (definitionId is not null) query = query.Where(x => x.DefinitionId == definitionId);
        if (runId is not null) query = query.Where(x => x.RunId == runId);
        if (kind is not null) query = query.Where(x => x.Kind == kind);
        if (severity is not null) query = query.Where(x => x.Severity == severity);
        if (eventCode is not null) query = query.Where(x => x.EventCode == eventCode);
        if (errorCode is not null) query = query.Where(x => x.ErrorCode == errorCode);
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProcessingOperationalLogDto(
                x.Id, x.DefinitionId, x.Definition!.Name, x.RunId, x.Kind, x.Severity, x.EventCode, x.Message,
                x.Attempt, x.MaxAttempts, x.ErrorCode, x.DurationMilliseconds, x.RecordImportJobId,
                x.ExternalExportJobId, x.OccurredAt))
            .ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<ProcessingOperationsSummaryDto> SummaryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        (var rangeFrom, var rangeTo) = Range(from, to);
        var runs = dbContext.ProcessingJobRuns.AsNoTracking().Where(x => x.CreatedAt >= rangeFrom && x.CreatedAt <= rangeTo);
        var statusCounts = await runs.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.LongCount() }).ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        var kindCounts = await runs.GroupBy(x => x.Definition!.Kind).Select(x => new { Kind = x.Key, Count = x.LongCount() }).ToDictionaryAsync(x => x.Kind, x => x.Count, ct);
        var events = dbContext.ProcessingOperationalLogs.AsNoTracking().Where(x => x.OccurredAt >= rangeFrom && x.OccurredAt <= rangeTo);
        var eventCounts = await events.Where(x => x.EventCode == ProcessingOperationalEventCodes.RetryScheduled
                || x.EventCode == ProcessingOperationalEventCodes.RetryExhausted
                || x.EventCode == ProcessingOperationalEventCodes.ScheduleSkippedActiveRun)
            .GroupBy(x => x.EventCode).Select(x => new { Code = x.Key, Count = x.LongCount() }).ToDictionaryAsync(x => x.Code, x => x.Count, ct);
        return new(rangeFrom, rangeTo,
            Get(statusCounts, ProcessingJobStatuses.Pending), Get(statusCounts, ProcessingJobStatuses.Running),
            Get(statusCounts, ProcessingJobStatuses.Succeeded), Get(statusCounts, ProcessingJobStatuses.Failed),
            Get(eventCounts, ProcessingOperationalEventCodes.RetryScheduled), Get(eventCounts, ProcessingOperationalEventCodes.RetryExhausted),
            Get(eventCounts, ProcessingOperationalEventCodes.ScheduleSkippedActiveRun), kindCounts);
    }

    public async Task<ProcessingJobPageDto<ProcessingNotificationRecipientDto>> ListRecipientsAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        (page, pageSize) = Page(page, pageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (search?.Length > 120) throw InvalidFilter("search");
        var query = dbContext.WorkspaceMemberships.AsNoTracking()
            .Where(x => x.Status == WorkspaceMembershipStatuses.Active && x.User != null && x.User.IsActive)
            .Where(x => x.User!.Roles.Any(userRole => userRole.Role != null && userRole.Role.IsActive
                && userRole.Role.Permissions.Any(rolePermission => rolePermission.Permission == PlatformPermissions.Integrations.Manage)))
            .Select(x => x.User!);
        if (search is not null)
        {
            var pattern = $"%{search}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Email, pattern));
        }
        query = query.Distinct();
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProcessingNotificationRecipientDto(x.Id, x.Name)).ToArrayAsync(ct);
        return new(items, page, pageSize, total);
    }

    public Task RecordQueuedAsync(ProcessingJobDefinition definition, ProcessingJobRun run, CancellationToken ct) =>
        InsertLogAsync(definition, run, ProcessingOperationalLogSeverities.Info, ProcessingOperationalEventCodes.RunQueued,
            "Processing run queued.", $"run:{run.Id}:queued", null, null, ct);

    public Task RecordStartedAsync(ProcessingJobDefinition definition, ProcessingJobRun run, CancellationToken ct) =>
        InsertLogAsync(definition, run, ProcessingOperationalLogSeverities.Info, ProcessingOperationalEventCodes.RunStarted,
            "Processing run started.", $"run:{run.Id}:started", null, null, ct);

    public Task RecordRetryScheduledAsync(ProcessingJobDefinition definition, ProcessingJobRun retry, CancellationToken ct) =>
        InsertLogAsync(definition, retry, ProcessingOperationalLogSeverities.Warning, ProcessingOperationalEventCodes.RetryScheduled,
            "A retry was scheduled for the failed processing run.", $"run:{retry.Id}:retry-scheduled", null, null, ct);

    public Task RecordScheduleSkippedAsync(ProcessingJobDefinition definition, DateTimeOffset dueAt, CancellationToken ct) =>
        InsertLogAsync(definition, null, ProcessingOperationalLogSeverities.Warning, ProcessingOperationalEventCodes.ScheduleSkippedActiveRun,
            "The scheduled occurrence was skipped because another run is active.", $"schedule:{definition.Id}:{dueAt.ToUnixTimeMilliseconds()}:active", null, null, ct);

    public async Task RecordTerminalAsync(ProcessingJobDefinition definition, ProcessingJobRun run, CancellationToken ct)
    {
        var succeeded = run.Status == ProcessingJobStatuses.Succeeded;
        var finalFailure = !succeeded
            && (definition.Kind == ProcessingJobKinds.CsvRecordImport || run.Attempt >= run.MaxAttempts);

        // Deliver before the idempotent terminal marker. If delivery fails unexpectedly,
        // reconciliation still sees the missing marker and can retry the whole safe step.
        if (finalFailure) await CreateFailureNotificationsAsync(definition, run, ct);

        await InsertLogAsync(definition, run,
            succeeded ? ProcessingOperationalLogSeverities.Info : ProcessingOperationalLogSeverities.Error,
            succeeded ? ProcessingOperationalEventCodes.RunSucceeded : ProcessingOperationalEventCodes.RunFailed,
            succeeded ? "Processing run succeeded." : "Processing run failed.",
            $"run:{run.Id}:terminal:{run.Status}", run.ErrorCode, Duration(run), ct);

        if (succeeded) return;
        if (run.ErrorCode == "import_recovery_unsafe")
            await InsertLogAsync(definition, run, ProcessingOperationalLogSeverities.Error,
                ProcessingOperationalEventCodes.ImportRecoveryUnsafe,
                "An interrupted CSV import was failed closed and was not replayed.",
                $"run:{run.Id}:import-recovery-unsafe", run.ErrorCode, Duration(run), ct);

        if (!finalFailure) return;
        if (definition.Kind == ProcessingJobKinds.RecordExport && run.MaxAttempts > 1)
            await InsertLogAsync(definition, run, ProcessingOperationalLogSeverities.Error,
                ProcessingOperationalEventCodes.RetryExhausted,
                "Processing run failed after its configured attempts.",
                $"run:{run.Id}:retry-exhausted", run.ErrorCode, Duration(run), ct);
    }

    public async Task<int> ReconcileTerminalAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);
        var candidates = await dbContext.ProcessingJobRuns.AsNoTracking()
            .Where(x => (x.Status == ProcessingJobStatuses.Succeeded || x.Status == ProcessingJobStatuses.Failed)
                && !dbContext.ProcessingOperationalLogs.Any(log => log.RunId == x.Id
                    && (log.EventCode == ProcessingOperationalEventCodes.RunSucceeded || log.EventCode == ProcessingOperationalEventCodes.RunFailed)))
            .OrderBy(x => x.CompletedAt).Take(limit).ToArrayAsync(ct);
        foreach (var run in candidates)
        {
            var definition = await dbContext.ProcessingJobDefinitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == run.DefinitionId, ct);
            if (definition is not null) await RecordTerminalAsync(definition, run, ct);
        }
        return candidates.Length;
    }

    public async Task<int> CleanupAsync(int retentionDays, int batchSize, CancellationToken ct)
    {
        retentionDays = Math.Clamp(retentionDays, 7, 365);
        batchSize = Math.Clamp(batchSize, 1, 500);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var ids = await dbContext.ProcessingOperationalLogs.AsNoTracking().Where(x => x.OccurredAt < cutoff)
            .OrderBy(x => x.OccurredAt).Select(x => x.Id).Take(batchSize).ToArrayAsync(ct);
        if (ids.Length == 0) return 0;
        return await dbContext.ProcessingOperationalLogs.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(ct);
    }

    private async Task InsertLogAsync(
        ProcessingJobDefinition definition,
        ProcessingJobRun? run,
        string severity,
        string eventCode,
        string message,
        string eventKey,
        string? errorCode,
        long? duration,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO processing_operational_logs
                    (id, workspace_id, definition_id, run_id, kind, severity, event_code, event_key, message,
                     attempt, max_attempts, error_code, duration_milliseconds, record_import_job_id, external_export_job_id, occurred_at, created_at)
                VALUES
                    ({Guid.NewGuid()}, {dbContext.ActiveWorkspaceId}, {definition.Id}, {(run == null ? null : run.Id)}, {definition.Kind}, {severity}, {eventCode}, {eventKey}, {message},
                     {(run == null ? null : run.Attempt)}, {(run == null ? null : run.MaxAttempts)}, {errorCode}, {duration},
                     {(run == null ? null : run.RecordImportJobId)}, {(run == null ? null : run.ExternalExportJobId)}, {now}, {now})
                ON CONFLICT (workspace_id, event_key) DO NOTHING", ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Processing operational log persistence failed for definition {DefinitionId} and run {RunId}.", definition.Id, run?.Id);
        }
    }

    private async Task CreateFailureNotificationsAsync(ProcessingJobDefinition definition, ProcessingJobRun run, CancellationToken ct)
    {
        var policy = Read<ProcessingFailureNotificationPolicyDefinition>(definition.FailureNotificationPolicyJson) ?? new();
        if (!policy.IsEnabled) return;
        var recipientIds = new HashSet<Guid>(policy.RecipientUserIds ?? Array.Empty<Guid>());
        if (policy.IncludeOwner) recipientIds.Add(definition.OwnerUserId);
        var activeIds = await dbContext.WorkspaceMemberships.AsNoTracking()
            .Where(x => recipientIds.Contains(x.UserId) && x.Status == WorkspaceMembershipStatuses.Active && x.User != null && x.User.IsActive)
            .Where(x => !dbContext.NotificationPreferences.Any(preference => preference.UserId == x.UserId && !preference.InAppEnabled))
            .Select(x => x.UserId).Distinct().ToArrayAsync(ct);
        var rootId = run.RetrySourceRunId ?? run.Id;
        var created = 0;
        var unauthorized = 0;
        var duplicates = 0;
        foreach (var userId in activeIds)
        {
            if (!await permissions.CanAsync(Principal(userId, dbContext.ActiveWorkspaceId), PlatformPermissions.Integrations.Manage, ct))
            {
                unauthorized++;
                continue;
            }
            var key = Hash($"processing_failure:{definition.Id}:{rootId}:{userId}");
            if (await dbContext.Notifications.AsNoTracking().AnyAsync(x => x.UserId == userId && x.DeduplicationKey == key, ct))
            {
                duplicates++;
                continue;
            }
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), UserId = userId, Title = "Processing job failed",
                Body = $"{definition.Name} failed after its available attempts. Review the processing run.",
                SourceType = "ProcessingJobRun", SourceId = run.Id, DeduplicationKey = key,
                MetadataJson = JsonSerializer.SerializeToDocument(new
                {
                    definitionId = definition.Id, runId = run.Id, retryRootRunId = rootId,
                    kind = definition.Kind, run.Attempt, run.MaxAttempts, run.ErrorCode
                }, JsonOptions),
                CreatedAt = DateTimeOffset.UtcNow
            });
            try
            {
                await dbContext.SaveChangesAsync(ct);
                created++;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                dbContext.ChangeTracker.Clear();
                duplicates++;
                logger.LogInformation("Duplicate processing failure notification suppressed for definition {DefinitionId} and run root {RunRootId}.", definition.Id, rootId);
            }
        }
        logger.LogInformation(
            "Processing failure notification delivery completed for definition {DefinitionId} and run root {RunRootId}: {ConfiguredCount} configured, {PreferenceAndMembershipEligibleCount} membership/preference eligible, {UnauthorizedCount} unauthorized, {CreatedCount} created, {DuplicateCount} duplicates suppressed.",
            definition.Id, rootId, recipientIds.Count, activeIds.Length, unauthorized, created, duplicates);
    }

    private static ClaimsPrincipal Principal(Guid userId, Guid workspaceId) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(WorkspaceClaims.WorkspaceId, workspaceId.ToString())
    }, "processing-notification"));
    private static T? Read<T>(JsonDocument? json) => json is null ? default : json.RootElement.Deserialize<T>(JsonOptions);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static long? Duration(ProcessingJobRun run) => run.StartedAt is { } started && run.CompletedAt is { } completed
        ? Math.Max(0, (long)(completed - started).TotalMilliseconds) : null;
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    private static long Get(IReadOnlyDictionary<string, long> values, string key) => values.TryGetValue(key, out var value) ? value : 0;
    private static (int, int) Page(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
    private static (DateTimeOffset, DateTimeOffset) Range(DateTimeOffset? from, DateTimeOffset? to)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddHours(-24);
        if (start > end || end - start > TimeSpan.FromDays(31))
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Processing operations date range must be ordered and no longer than 31 days.");
        return (start, end);
    }
    private static ProcessingJobException InvalidFilter(string name) => new(StatusCodes.Status400BadRequest, $"Processing operations filter '{name}' is invalid.");
}
