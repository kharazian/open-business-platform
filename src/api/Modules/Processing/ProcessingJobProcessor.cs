using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Integrations;
using OpenBusinessPlatform.Api.Modules.Reports;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public sealed class ProcessingJobProcessor(
    OpenBusinessPlatformDbContext dbContext,
    RecordImportJobService imports,
    ExternalExportJobService exports)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static TimeSpan ClaimLease { get; } = TimeSpan.FromMinutes(5);

    public async Task<int> EnqueueDueSchedulesAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var ids = await dbContext.ProcessingJobDefinitions.AsNoTracking()
            .Where(x => x.IsEnabled && !x.IsDeleted && x.NextRunAt != null && x.NextRunAt <= now
                && (x.ScheduleLockedAt == null || x.ScheduleLockedAt < now - ClaimLease))
            .OrderBy(x => x.NextRunAt).Select(x => x.Id).Take(10).ToArrayAsync(ct);
        var count = 0;
        foreach (var id in ids)
        {
            var claimId = Guid.NewGuid();
            var lockedAt = DateTimeOffset.UtcNow;
            var claimed = await dbContext.ProcessingJobDefinitions.Where(x => x.Id == id && x.IsEnabled && !x.IsDeleted
                    && x.NextRunAt <= lockedAt && (x.ScheduleLockedAt == null || x.ScheduleLockedAt < lockedAt - ClaimLease))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ScheduleClaimId, claimId).SetProperty(x => x.ScheduleLockedAt, lockedAt), ct);
            if (claimed == 0) continue;
            var definition = await dbContext.ProcessingJobDefinitions.AsNoTracking().SingleAsync(x => x.Id == id, ct);
            var schedule = Read<ProcessingJobScheduleDefinition>(definition.ScheduleJson);
            var next = schedule is null ? null : RecurringScheduleCalculator.CalculateNextRun(
                new(schedule.Kind, schedule.TimeZone, schedule.StartAt, schedule.Interval, schedule.DayOfWeek, schedule.DayOfMonth), lockedAt);
            var active = await dbContext.ProcessingJobRuns.AnyAsync(x => x.DefinitionId == id && ProcessingJobStatuses.Active.Contains(x.Status), ct);
            if (!active)
            {
                var policy = Read<ProcessingJobRetryPolicyDefinition>(definition.RetryPolicyJson) ?? new();
                var run = NewRun(definition, ProcessingJobRunSources.Scheduled, 1, policy.IsEnabled ? policy.MaxAttempts : 1, definition.OwnerUserId, lockedAt);
                dbContext.ProcessingJobRuns.Add(run);
                AddAudit(id, "processing_job_scheduled_enqueued", definition.OwnerUserId, new { runId = run.Id });
                try { await dbContext.SaveChangesAsync(ct); }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { dbContext.ChangeTracker.Clear(); }
            }
            await dbContext.ProcessingJobDefinitions.Where(x => x.Id == id && x.ScheduleClaimId == claimId && x.ScheduleLockedAt == lockedAt)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextRunAt, next).SetProperty(x => x.IsEnabled, next != null)
                    .SetProperty(x => x.ScheduleClaimId, (Guid?)null).SetProperty(x => x.ScheduleLockedAt, (DateTimeOffset?)null), ct);
            count++;
        }
        return count;
    }

    public async Task<int> ProcessRunsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = await dbContext.ProcessingJobRuns.AsNoTracking()
            .Where(x => x.NextAttemptAt <= now && (x.Status == ProcessingJobStatuses.Pending
                || x.Status == ProcessingJobStatuses.Running && x.LockedAt < now - ClaimLease))
            .OrderBy(x => x.NextAttemptAt).ThenBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.DefinitionId,
                x.Status,
                x.ClaimId,
                x.LockedAt,
                Kind = x.Definition!.Kind,
                OwnerUserId = x.Definition.OwnerUserId
            })
            .Take(5)
            .ToArrayAsync(ct);
        var count = 0;
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            if (candidate.Status == ProcessingJobStatuses.Running
                && candidate.Kind == ProcessingJobKinds.CsvRecordImport)
            {
                var completedAt = DateTimeOffset.UtcNow;
                var failed = await dbContext.ProcessingJobRuns
                    .Where(x => x.Id == candidate.Id
                        && x.Status == ProcessingJobStatuses.Running
                        && x.ClaimId == candidate.ClaimId
                        && x.LockedAt == candidate.LockedAt)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, ProcessingJobStatuses.Failed)
                        .SetProperty(x => x.CompletedAt, completedAt)
                        .SetProperty(x => x.LockedAt, (DateTimeOffset?)null)
                        .SetProperty(x => x.ClaimId, (Guid?)null)
                        .SetProperty(x => x.InputContent, (string?)null)
                        .SetProperty(x => x.ErrorCode, "import_recovery_unsafe")
                        .SetProperty(x => x.ErrorMessage, "An interrupted CSV import cannot be replayed safely."), ct);
                if (failed > 0)
                {
                    AddAudit(candidate.DefinitionId, "processing_job_run_failed", candidate.OwnerUserId,
                        new { runId = candidate.Id, errorCode = "import_recovery_unsafe" });
                    await dbContext.SaveChangesAsync(ct);
                    count++;
                }
                continue;
            }

            var claimId = Guid.NewGuid();
            var claimedAt = DateTimeOffset.UtcNow;
            var claimed = await dbContext.ProcessingJobRuns.Where(x => x.Id == candidate.Id && x.NextAttemptAt <= claimedAt
                    && (x.Status == ProcessingJobStatuses.Pending || x.Status == ProcessingJobStatuses.Running && x.LockedAt < claimedAt - ClaimLease))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProcessingJobStatuses.Running)
                    .SetProperty(x => x.ClaimId, claimId).SetProperty(x => x.LockedAt, claimedAt)
                    .SetProperty(x => x.StartedAt, x => x.StartedAt ?? claimedAt), ct);
            if (claimed == 0) continue;
            await ExecuteAsync(candidate.Id, claimId, ct);
            count++;
        }
        return count;
    }

    private async Task ExecuteAsync(Guid runId, Guid claimId, CancellationToken ct)
    {
        var run = await dbContext.ProcessingJobRuns.AsNoTracking().SingleAsync(x => x.Id == runId, ct);
        var definition = await dbContext.ProcessingJobDefinitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == run.DefinitionId && !x.IsDeleted, ct);
        try
        {
            if (definition is null) throw new ProcessingRunFailure("definition_unavailable", "Processing job is no longer available.");
            await EnsureActorAsync(definition.OwnerUserId, ct);
            var principal = Principal(definition.OwnerUserId, dbContext.ActiveWorkspaceId);
            var config = Read<ProcessingJobConfigDefinition>(definition.ConfigJson)
                ?? throw new ProcessingRunFailure("config_invalid", "Processing job configuration is invalid.");
            Guid? importId = null, exportId = null;
            string? terminalErrorCode = null, terminalErrorMessage = null;
            if (definition.Kind == ProcessingJobKinds.CsvRecordImport)
            {
                if (string.IsNullOrEmpty(run.InputContent) || config.Mapping is null) throw new ProcessingRunFailure("input_unavailable", "Queued CSV input is unavailable.");
                var result = await imports.CreateAsync(principal, new(config.FormId, config.IntegrationKey, run.InputFileName, run.InputContent, config.Mapping), definition.OwnerUserId, ct);
                importId = result.Id;
                if (result.Status != RecordImportJobStatuses.Succeeded)
                {
                    terminalErrorCode = result.Status == RecordImportJobStatuses.CompletedWithErrors
                        ? "import_completed_with_errors"
                        : "import_failed";
                    terminalErrorMessage = "The CSV import completed with row errors. Review the linked import job.";
                }
            }
            else
            {
                var result = await exports.CreateAsync(principal, new(config.SourceType!, config.Format!, config.IntegrationKey, config.FormId, config.ReportId, config.Search, config.MaxRows), definition.OwnerUserId, ct);
                exportId = result.Id;
            }
            var completedAt = DateTimeOffset.UtcNow;
            var terminalStatus = terminalErrorCode is null ? ProcessingJobStatuses.Succeeded : ProcessingJobStatuses.Failed;
            var updated = await dbContext.ProcessingJobRuns.Where(x => x.Id == runId && x.ClaimId == claimId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, terminalStatus)
                    .SetProperty(x => x.CompletedAt, completedAt).SetProperty(x => x.LockedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.ClaimId, (Guid?)null).SetProperty(x => x.InputContent, (string?)null)
                    .SetProperty(x => x.RecordImportJobId, importId).SetProperty(x => x.ExternalExportJobId, exportId)
                    .SetProperty(x => x.ErrorCode, terminalErrorCode).SetProperty(x => x.ErrorMessage, terminalErrorMessage), ct);
            if (updated > 0)
            {
                AddAudit(run.DefinitionId,
                    terminalStatus == ProcessingJobStatuses.Succeeded ? "processing_job_run_succeeded" : "processing_job_run_failed",
                    definition.OwnerUserId,
                    new { runId, errorCode = terminalErrorCode });
                await dbContext.SaveChangesAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            var (code, message) = SafeError(ex);
            var completedAt = DateTimeOffset.UtcNow;
            var updated = await dbContext.ProcessingJobRuns.Where(x => x.Id == runId && x.ClaimId == claimId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProcessingJobStatuses.Failed)
                    .SetProperty(x => x.CompletedAt, completedAt).SetProperty(x => x.LockedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.ClaimId, (Guid?)null).SetProperty(x => x.InputContent, (string?)null)
                    .SetProperty(x => x.ErrorCode, code).SetProperty(x => x.ErrorMessage, message), ct);
            if (updated == 0 || definition is null) return;
            AddAudit(run.DefinitionId, "processing_job_run_failed", definition.OwnerUserId, new { runId, errorCode = code });
            await dbContext.SaveChangesAsync(ct);
            await QueueAutomaticRetryAsync(definition, run, completedAt, ct);
        }
    }

    private async Task QueueAutomaticRetryAsync(ProcessingJobDefinition definition, ProcessingJobRun failed, DateTimeOffset now, CancellationToken ct)
    {
        var policy = Read<ProcessingJobRetryPolicyDefinition>(definition.RetryPolicyJson) ?? new();
        if (definition.Kind != ProcessingJobKinds.RecordExport || !policy.IsEnabled || failed.Attempt >= policy.MaxAttempts) return;
        var retry = NewRun(definition, ProcessingJobRunSources.Retry, failed.Attempt + 1, policy.MaxAttempts, definition.OwnerUserId, now.AddSeconds(policy.DelaySeconds));
        retry.RetrySourceRunId = failed.RetrySourceRunId ?? failed.Id;
        dbContext.ProcessingJobRuns.Add(retry);
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { dbContext.ChangeTracker.Clear(); }
    }

    private async Task EnsureActorAsync(Guid userId, CancellationToken ct)
    {
        var valid = await dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive, ct)
            && await dbContext.WorkspaceMemberships.AsNoTracking().AnyAsync(x => x.UserId == userId && x.WorkspaceId == dbContext.ActiveWorkspaceId && x.Status == WorkspaceMembershipStatuses.Active, ct);
        if (!valid) throw new ProcessingRunFailure("actor_unavailable", "The processing job owner is not an active workspace member.");
    }

    private static ClaimsPrincipal Principal(Guid userId, Guid workspaceId) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(WorkspaceClaims.WorkspaceId, workspaceId.ToString())
    }, "processing-job"));
    private static T? Read<T>(JsonDocument? json) => json is null ? default : json.RootElement.Deserialize<T>(JsonOptions);
    private static ProcessingJobRun NewRun(ProcessingJobDefinition d, string source, int attempt, int max, Guid actor, DateTimeOffset at) => new() { Id = Guid.NewGuid(), DefinitionId = d.Id, Source = source, Status = ProcessingJobStatuses.Pending, Attempt = attempt, MaxAttempts = max, NextAttemptAt = at, CreatedById = actor };
    private void AddAudit(Guid id, string action, Guid user, object metadata) => dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "ProcessingJobDefinition", EntityId = id, Action = action, UserId = user, MetadataJson = JsonSerializer.SerializeToDocument(metadata, JsonOptions) });
    private static (string, string) SafeError(Exception ex) => ex switch
    {
        ProcessingRunFailure failure => (failure.Code, Limit(failure.Message)),
        ExternalExportException export when export.Message == "source_limit_exceeded" => ("source_limit_exceeded", "The authorized source exceeds the configured row limit."),
        ExternalExportException export => ("export_failed", Limit(export.Message)),
        ReportManagementException report when report.Message == "source_limit_exceeded" => ("source_limit_exceeded", "The authorized source exceeds the configured row limit."),
        RecordImportException import => ("import_failed", Limit(import.Message)),
        _ => ("processing_failed", "Processing failed. Review the source configuration and permissions.")
    };
    private static string Limit(string value) => value.Length <= 1000 ? value : value[..1000];
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    private sealed class ProcessingRunFailure(string code, string message) : Exception(message) { public string Code { get; } = code; }
}

public sealed class ProcessingJobWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessingJobWorker> logger,
    IOptions<ProcessingJobOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(options.Value.PollingIntervalSeconds, 5, 300)));
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOnceAsync(stoppingToken);
            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    internal async Task ProcessOnceAsync(CancellationToken ct)
    {
        try
        {
            Guid[] workspaceIds;
            await using (var discovery = scopeFactory.CreateAsyncScope())
            {
                var db = discovery.ServiceProvider.GetRequiredService<OpenBusinessPlatformDbContext>();
                workspaceIds = await db.Workspaces.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToArrayAsync(ct);
            }
            foreach (var workspaceId in workspaceIds)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                scope.ServiceProvider.GetRequiredService<BackgroundWorkspaceContext>().WorkspaceId = workspaceId;
                var processor = scope.ServiceProvider.GetRequiredService<ProcessingJobProcessor>();
                await processor.EnqueueDueSchedulesAsync(ct);
                await processor.ProcessRunsAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Processing job worker pass failed."); }
    }
}
