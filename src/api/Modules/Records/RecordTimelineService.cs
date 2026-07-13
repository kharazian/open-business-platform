using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordTimelineService
{
    private readonly OpenBusinessPlatformDbContext dbContext;

    public RecordTimelineService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<RecordTimelineDto> ListTimelineAsync(
        ClaimsPrincipal principal,
        Guid recordId,
        int requestedLimit,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.Records
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordQueryException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new RecordQueryException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        var limit = Math.Clamp(requestedLimit, 1, 50);
        var auditEntries = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.EntityType == "Record" && entry.EntityId == recordId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        var workflowEntries = await dbContext.WorkflowHistory
            .AsNoTracking()
            .Where(entry => entry.RecordId == recordId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        var triggerEntries = await dbContext.TriggerLogs
            .AsNoTracking()
            .Where(entry => entry.EntityType == "Record" && entry.EntityId == recordId)
            .OrderByDescending(entry => entry.StartedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        var integrationEntries = await dbContext.IntegrationLogs
            .AsNoTracking()
            .Where(entry =>
                (entry.TargetEntityType == "Record" && entry.TargetEntityId == recordId)
                || (entry.SourceType == "Record" && entry.SourceId == recordId))
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        var items = auditEntries.Select(ToTimelineEntry)
            .Concat(workflowEntries.Select(ToTimelineEntry))
            .Concat(triggerEntries.Select(ToTimelineEntry))
            .Concat(integrationEntries.Select(ToTimelineEntry))
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenBy(entry => entry.Source, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();

        return new RecordTimelineDto(recordId, items);
    }

    private static RecordTimelineEntryDto ToTimelineEntry(AuditLogEntry entry)
    {
        return new RecordTimelineEntryDto(
            $"audit:{entry.Id:N}",
            RecordTimelineSources.Audit,
            entry.Action,
            null,
            FormatAction(entry.Action),
            entry.CreatedAt,
            entry.UserId);
    }

    private static RecordTimelineEntryDto ToTimelineEntry(WorkflowHistoryEntry entry)
    {
        var summary = string.IsNullOrWhiteSpace(entry.FromStateKey)
            ? $"{FormatAction(entry.Action)} to {entry.ToStateKey}"
            : $"{FormatAction(entry.Action)} from {entry.FromStateKey} to {entry.ToStateKey}";

        return new RecordTimelineEntryDto(
            $"workflow:{entry.Id:N}",
            RecordTimelineSources.Workflow,
            entry.Action,
            entry.TransitionKey,
            summary,
            entry.CreatedAt,
            entry.ActorUserId);
    }

    private static RecordTimelineEntryDto ToTimelineEntry(TriggerExecutionLog entry)
    {
        return new RecordTimelineEntryDto(
            $"trigger:{entry.Id:N}",
            RecordTimelineSources.Trigger,
            entry.EventName,
            entry.Status,
            $"{FormatAction(entry.EventName)} trigger {entry.Status}",
            entry.StartedAt,
            null);
    }

    private static RecordTimelineEntryDto ToTimelineEntry(IntegrationLogEntry entry)
    {
        return new RecordTimelineEntryDto(
            $"integration:{entry.Id:N}",
            RecordTimelineSources.Integration,
            entry.IntegrationType,
            entry.Status,
            $"{FormatAction(entry.Direction)} {FormatAction(entry.IntegrationType)} via {entry.IntegrationKey}",
            entry.CreatedAt,
            entry.CreatedById);
    }

    private static string FormatAction(string value)
    {
        return string.Join(" ", value.Split(new[] { '_', '.', '-' }, StringSplitOptions.RemoveEmptyEntries));
    }
}
