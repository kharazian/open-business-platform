using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public sealed class ProcessingJobService(OpenBusinessPlatformDbContext dbContext, ProcessingOperationsService operations)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProcessingJobPageDto<ProcessingJobSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Page(page, pageSize);
        var query = dbContext.ProcessingJobDefinitions.AsNoTracking().Where(x => !x.IsDeleted);
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(items.Select(ToSummary).ToArray(), page, pageSize, total);
    }

    public async Task<ProcessingJobDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var job = await dbContext.ProcessingJobDefinitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return job is null ? null : ToDetail(job);
    }

    public async Task<ProcessingJobDetailDto> CreateAsync(CreateProcessingJobRequest request, Guid actorId, CancellationToken ct)
    {
        var normalized = Normalize(request);
        ThrowIfInvalid(ProcessingJobValidator.Validate(normalized));
        await EnsurePersistentActorAsync(actorId, ct);
        await EnsureSourcesAsync(normalized.Config, ct);
        await EnsureNotificationRecipientsAsync(normalized.FailureNotificationPolicy, ct);
        var now = DateTimeOffset.UtcNow;
        var entity = new ProcessingJobDefinition
        {
            Id = Guid.NewGuid(), Name = normalized.Name, Kind = normalized.Kind,
            ConfigJson = JsonSerializer.SerializeToDocument(normalized.Config, JsonOptions),
            ScheduleJson = normalized.Schedule is null ? null : JsonSerializer.SerializeToDocument(normalized.Schedule, JsonOptions),
            RetryPolicyJson = JsonSerializer.SerializeToDocument(normalized.RetryPolicy ?? new ProcessingJobRetryPolicyDefinition(), JsonOptions),
            FailureNotificationPolicyJson = JsonSerializer.SerializeToDocument(normalized.FailureNotificationPolicy ?? new ProcessingFailureNotificationPolicyDefinition(), JsonOptions),
            IsEnabled = normalized.IsEnabled, OwnerUserId = actorId, FormId = normalized.Config.FormId,
            ReportId = normalized.Config.ReportId,
            NextRunAt = normalized.IsEnabled ? Next(normalized.Schedule, now) : null,
            CreatedById = actorId
        };
        if (entity.IsEnabled && normalized.Schedule is null)
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Enabled processing jobs require a schedule.");
        if (entity.IsEnabled && entity.NextRunAt is null)
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Enabled processing jobs require a future schedule occurrence.");
        dbContext.ProcessingJobDefinitions.Add(entity);
        Audit(entity.Id, "processing_job_created", actorId, new { entity.Name, entity.Kind });
        await dbContext.SaveChangesAsync(ct);
        return ToDetail(entity);
    }

    public async Task<ProcessingJobDetailDto> UpdateAsync(Guid id, UpdateProcessingJobRequest request, Guid? actorId, CancellationToken ct)
    {
        if (request.AdditionalProperties is { Count: > 0 })
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Request contains unsupported properties.");
        var entity = await FindAsync(id, ct);
        EnsureStamp(entity, request.ConcurrencyStamp);
        var candidate = Normalize(new CreateProcessingJobRequest(request.Name, entity.Kind, request.Config, request.Schedule, request.RetryPolicy, entity.IsEnabled, request.FailureNotificationPolicy));
        ThrowIfInvalid(ProcessingJobValidator.Validate(candidate));
        await EnsureSourcesAsync(candidate.Config, ct);
        await EnsureNotificationRecipientsAsync(candidate.FailureNotificationPolicy, ct);
        if (entity.IsEnabled && candidate.Schedule is null)
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Enabled processing jobs require a schedule.");
        entity.Name = candidate.Name;
        entity.ConfigJson = JsonSerializer.SerializeToDocument(candidate.Config, JsonOptions);
        entity.ScheduleJson = candidate.Schedule is null ? null : JsonSerializer.SerializeToDocument(candidate.Schedule, JsonOptions);
        entity.RetryPolicyJson = JsonSerializer.SerializeToDocument(candidate.RetryPolicy ?? new ProcessingJobRetryPolicyDefinition(), JsonOptions);
        entity.FailureNotificationPolicyJson = JsonSerializer.SerializeToDocument(candidate.FailureNotificationPolicy ?? new ProcessingFailureNotificationPolicyDefinition(), JsonOptions);
        entity.FormId = candidate.Config.FormId;
        entity.ReportId = candidate.Config.ReportId;
        entity.NextRunAt = entity.IsEnabled ? Next(candidate.Schedule, DateTimeOffset.UtcNow) : null;
        if (entity.IsEnabled && entity.NextRunAt is null)
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Enabled processing jobs require a future schedule occurrence.");
        entity.UpdatedById = actorId;
        Audit(id, "processing_job_updated", actorId, new { entity.Name, entity.Kind });
        await dbContext.SaveChangesAsync(ct);
        return ToDetail(entity);
    }

    public async Task DeleteAsync(Guid id, ProcessingJobStateRequest request, Guid? actorId, CancellationToken ct)
    {
        var entity = await FindAsync(id, ct);
        EnsureStamp(entity, request.ConcurrencyStamp);
        if (await HasActiveRunAsync(id, ct)) throw Conflict();
        entity.IsDeleted = true; entity.DeletedAt = DateTimeOffset.UtcNow; entity.DeletedById = actorId;
        entity.IsEnabled = false; entity.NextRunAt = null; entity.ScheduleClaimId = null; entity.ScheduleLockedAt = null;
        Audit(id, "processing_job_deleted", actorId, new { entity.Name, entity.Kind });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<ProcessingJobDetailDto> SetEnabledAsync(Guid id, ProcessingJobStateRequest request, bool enabled, Guid? actorId, CancellationToken ct)
    {
        var entity = await FindAsync(id, ct);
        EnsureStamp(entity, request.ConcurrencyStamp);
        var schedule = Deserialize<ProcessingJobScheduleDefinition>(entity.ScheduleJson);
        if (enabled && schedule is null) throw new ProcessingJobException(StatusCodes.Status409Conflict, "A schedule is required before enabling this job.");
        entity.IsEnabled = enabled;
        entity.NextRunAt = enabled ? Next(schedule, DateTimeOffset.UtcNow) : null;
        if (enabled && entity.NextRunAt is null)
            throw new ProcessingJobException(StatusCodes.Status409Conflict, "The schedule has no future occurrence.");
        entity.ScheduleClaimId = null; entity.ScheduleLockedAt = null; entity.UpdatedById = actorId;
        Audit(id, enabled ? "processing_job_enabled" : "processing_job_disabled", actorId, new { entity.Name });
        await dbContext.SaveChangesAsync(ct);
        return ToDetail(entity);
    }

    public async Task<ProcessingJobRunDto> QueueManualAsync(Guid id, CreateProcessingJobRunRequest request, Guid actorId, CancellationToken ct)
    {
        var definition = await FindAsync(id, ct);
        await EnsurePersistentActorAsync(actorId, ct);
        ThrowIfInvalid(ProcessingJobValidator.ValidateManualRun(definition.Kind, request));
        var policy = Deserialize<ProcessingJobRetryPolicyDefinition>(definition.RetryPolicyJson) ?? new();
        var content = definition.Kind == ProcessingJobKinds.CsvRecordImport ? request.CsvContent : null;
        var run = NewRun(definition, ProcessingJobRunSources.Manual, 1, policy.IsEnabled ? policy.MaxAttempts : 1, actorId);
        if (content is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            run.InputContent = content; run.InputFileName = string.IsNullOrWhiteSpace(request.FileName) ? "import.csv" : Path.GetFileName(request.FileName);
            run.InputSizeBytes = bytes.LongLength; run.InputChecksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        dbContext.ProcessingJobRuns.Add(run);
        Audit(id, "processing_job_manual_run_requested", actorId, new { runId = run.Id, run.Source });
        await SaveQueueAsync(ct);
        await operations.RecordQueuedAsync(definition, run, ct);
        return ToRun(run);
    }

    public async Task<ProcessingJobRunDto> RetryAsync(Guid definitionId, Guid runId, Guid actorId, CancellationToken ct)
    {
        var definition = await FindAsync(definitionId, ct);
        await EnsurePersistentActorAsync(actorId, ct);
        var previous = await dbContext.ProcessingJobRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == runId && x.DefinitionId == definitionId, ct)
            ?? throw new ProcessingJobException(StatusCodes.Status404NotFound, "Processing run was not found.");
        var policy = Deserialize<ProcessingJobRetryPolicyDefinition>(definition.RetryPolicyJson) ?? new();
        if (definition.Kind != ProcessingJobKinds.RecordExport || previous.Status != ProcessingJobStatuses.Failed || !policy.IsEnabled || previous.Attempt >= policy.MaxAttempts)
            throw new ProcessingJobException(StatusCodes.Status409Conflict, "This run is not eligible for retry.");
        var retryRootId = previous.RetrySourceRunId ?? previous.Id;
        var latestAttempt = await dbContext.ProcessingJobRuns.AsNoTracking()
            .Where(x => x.Id == retryRootId || x.RetrySourceRunId == retryRootId)
            .MaxAsync(x => x.Attempt, ct);
        if (latestAttempt >= policy.MaxAttempts)
            throw new ProcessingJobException(StatusCodes.Status409Conflict, "This retry chain has exhausted its configured attempts.");
        var run = NewRun(definition, ProcessingJobRunSources.Retry, latestAttempt + 1, policy.MaxAttempts, actorId);
        run.RetrySourceRunId = retryRootId;
        dbContext.ProcessingJobRuns.Add(run);
        Audit(definitionId, "processing_job_retry_requested", actorId, new { runId = run.Id, retrySourceRunId = run.RetrySourceRunId, run.Attempt });
        await SaveQueueAsync(ct);
        await operations.RecordQueuedAsync(definition, run, ct);
        await operations.RecordRetryScheduledAsync(definition, run, ct);
        return ToRun(run);
    }

    public async Task<ProcessingJobPageDto<ProcessingJobRunDto>> ListRunsAsync(Guid definitionId, int page, int pageSize, CancellationToken ct)
    {
        if (!await dbContext.ProcessingJobDefinitions.AnyAsync(x => x.Id == definitionId && !x.IsDeleted, ct))
            throw new ProcessingJobException(StatusCodes.Status404NotFound, "Processing job was not found.");
        (page, pageSize) = Page(page, pageSize);
        var query = dbContext.ProcessingJobRuns.AsNoTracking().Where(x => x.DefinitionId == definitionId);
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(rows.Select(ToRun).ToArray(), page, pageSize, total);
    }

    public async Task<ProcessingJobRunDto?> GetRunAsync(Guid definitionId, Guid runId, CancellationToken ct)
    {
        if (!await dbContext.ProcessingJobDefinitions.AnyAsync(x => x.Id == definitionId && !x.IsDeleted, ct)) return null;
        var run = await dbContext.ProcessingJobRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == runId && x.DefinitionId == definitionId, ct);
        return run is null ? null : ToRun(run);
    }

    private async Task EnsurePersistentActorAsync(Guid actorId, CancellationToken ct)
    {
        var workspaceId = dbContext.ActiveWorkspaceId;
        var valid = await dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == actorId && x.IsActive, ct)
            && await dbContext.WorkspaceMemberships.AsNoTracking().AnyAsync(x => x.UserId == actorId && x.WorkspaceId == workspaceId && x.Status == WorkspaceMembershipStatuses.Active, ct);
        if (!valid) throw new ProcessingJobException(StatusCodes.Status403Forbidden, "An active persistent workspace user is required.");
    }

    private async Task EnsureSourcesAsync(ProcessingJobConfigDefinition config, CancellationToken ct)
    {
        if (!await dbContext.Forms.AsNoTracking().AnyAsync(x => x.Id == config.FormId && !x.IsDeleted, ct))
            throw new ProcessingJobException(StatusCodes.Status404NotFound, "Source form was not found.");
        if (config.ReportId is { } reportId && !await dbContext.Reports.AsNoTracking().AnyAsync(x => x.Id == reportId && x.FormId == config.FormId && !x.IsDeleted, ct))
            throw new ProcessingJobException(StatusCodes.Status404NotFound, "Source report was not found.");
    }

    private async Task EnsureNotificationRecipientsAsync(ProcessingFailureNotificationPolicyDefinition? policy, CancellationToken ct)
    {
        var ids = policy?.RecipientUserIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (ids.Length == 0) return;
        var count = await dbContext.WorkspaceMemberships.AsNoTracking()
            .Where(x => ids.Contains(x.UserId) && x.Status == WorkspaceMembershipStatuses.Active && x.User != null && x.User.IsActive)
            .Select(x => x.UserId).Distinct().CountAsync(ct);
        if (count != ids.Length)
            throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Failure notification recipients must be active users in the current workspace.");
    }

    private async Task<ProcessingJobDefinition> FindAsync(Guid id, CancellationToken ct) =>
        await dbContext.ProcessingJobDefinitions.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
        ?? throw new ProcessingJobException(StatusCodes.Status404NotFound, "Processing job was not found.");

    private async Task<bool> HasActiveRunAsync(Guid id, CancellationToken ct) => await dbContext.ProcessingJobRuns.AnyAsync(x => x.DefinitionId == id && ProcessingJobStatuses.Active.Contains(x.Status), ct);
    private static ProcessingJobException Conflict() => new(StatusCodes.Status409Conflict, "This processing job already has an active run.");
    private async Task SaveQueueAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw Conflict();
        }
    }
    private static void ThrowIfInvalid(ProcessingJobValidationResult result) { if (!result.Valid) throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Processing job is invalid.", result.Errors); }
    private static void EnsureStamp(ProcessingJobDefinition x, string stamp) { if (string.IsNullOrWhiteSpace(stamp) || x.ConcurrencyStamp != stamp.Trim()) throw new ProcessingJobException(StatusCodes.Status409Conflict, "Processing job changed. Refresh and try again."); }
    private static (int, int) Page(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
    private static ProcessingJobRun NewRun(ProcessingJobDefinition d, string source, int attempt, int maxAttempts, Guid actor) => new() { Id = Guid.NewGuid(), DefinitionId = d.Id, Source = source, Status = ProcessingJobStatuses.Pending, Attempt = attempt, MaxAttempts = maxAttempts, NextAttemptAt = DateTimeOffset.UtcNow, CreatedById = actor };
    private static T? Deserialize<T>(JsonDocument? json) => json is null ? default : json.RootElement.Deserialize<T>(JsonOptions);
    private static DateTimeOffset? Next(ProcessingJobScheduleDefinition? s, DateTimeOffset now) => s is null ? null : RecurringScheduleCalculator.CalculateNextRun(new(s.Kind, s.TimeZone, s.StartAt, s.Interval, s.DayOfWeek, s.DayOfMonth), now);
    private static CreateProcessingJobRequest Normalize(CreateProcessingJobRequest r)
    {
        if (r.Config is null) throw new ProcessingJobException(StatusCodes.Status400BadRequest, "Job config is required.");
        return r with
        {
            Name = r.Name?.Trim() ?? string.Empty,
            Kind = r.Kind?.Trim().ToLowerInvariant() ?? string.Empty,
            Config = r.Config with
            {
                IntegrationKey = r.Config.IntegrationKey?.Trim().ToLowerInvariant() ?? string.Empty,
                SourceType = r.Config.SourceType?.Trim().ToLowerInvariant(),
                Format = r.Config.Format?.Trim().ToLowerInvariant(),
                Search = string.IsNullOrWhiteSpace(r.Config.Search) ? null : r.Config.Search.Trim()
            },
            Schedule = r.Schedule is null ? null : r.Schedule with
            {
                Kind = r.Schedule.Kind?.Trim().ToLowerInvariant() ?? string.Empty,
                TimeZone = r.Schedule.TimeZone?.Trim() ?? string.Empty
            },
            RetryPolicy = r.RetryPolicy ?? new(),
            FailureNotificationPolicy = r.FailureNotificationPolicy is null ? new() : r.FailureNotificationPolicy with
            {
                RecipientUserIds = r.FailureNotificationPolicy.RecipientUserIds?.ToArray() ?? Array.Empty<Guid>()
            }
        };
    }
    private void Audit(Guid id, string action, Guid? actor, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "ProcessingJobDefinition", EntityId = id, Action = action, UserId = actor, MetadataJson = JsonSerializer.SerializeToDocument(metadata, JsonOptions) });

    internal static ProcessingJobSummaryDto ToSummary(ProcessingJobDefinition x) => new(x.Id, x.Name, x.Kind, x.IsEnabled, x.FormId, x.ReportId, x.NextRunAt, x.ConcurrencyStamp, x.CreatedAt, x.UpdatedAt);
    internal static ProcessingJobDetailDto ToDetail(ProcessingJobDefinition x) => new(x.Id, x.Name, x.Kind, Deserialize<ProcessingJobConfigDefinition>(x.ConfigJson)!, Deserialize<ProcessingJobScheduleDefinition>(x.ScheduleJson), Deserialize<ProcessingJobRetryPolicyDefinition>(x.RetryPolicyJson) ?? new(), Deserialize<ProcessingFailureNotificationPolicyDefinition>(x.FailureNotificationPolicyJson) ?? new(), x.IsEnabled, x.OwnerUserId, x.NextRunAt, x.ConcurrencyStamp, x.CreatedAt, x.CreatedById, x.UpdatedAt, x.UpdatedById);
    internal static ProcessingJobRunDto ToRun(ProcessingJobRun x) => new(x.Id, x.DefinitionId, x.Source, x.Status, x.Attempt, x.MaxAttempts, x.NextAttemptAt, x.StartedAt, x.CompletedAt, x.ErrorCode, x.ErrorMessage, x.InputFileName, x.InputSizeBytes, x.InputChecksum, x.RecordImportJobId, x.ExternalExportJobId, x.RetrySourceRunId, x.CreatedAt, x.CreatedById);
}
