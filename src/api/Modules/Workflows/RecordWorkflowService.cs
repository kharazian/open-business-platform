using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Triggers;

namespace OpenBusinessPlatform.Api.Modules.Workflows;

public sealed class RecordWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int HistoryLimit = 25;
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerEventDispatcher triggerDispatcher;

    public RecordWorkflowService(OpenBusinessPlatformDbContext dbContext, TriggerEventDispatcher triggerDispatcher)
    {
        this.dbContext = dbContext;
        this.triggerDispatcher = triggerDispatcher;
    }

    public static IReadOnlyList<RecordWorkflowTransitionDto> GetAvailableDirectTransitions(
        WorkflowDefinitionConfig config,
        string? stateKey)
    {
        if (string.IsNullOrWhiteSpace(stateKey))
        {
            return Array.Empty<RecordWorkflowTransitionDto>();
        }

        var normalized = WorkflowDefinitionValidator.NormalizeConfig(config);
        return normalized.Transitions
            .Where(transition => string.Equals(transition.FromStateKey, stateKey, StringComparison.Ordinal))
            .Where(transition => string.IsNullOrWhiteSpace(transition.ApprovalStepKey))
            .Select(transition => new RecordWorkflowTransitionDto(
                transition.Key,
                transition.Name,
                transition.FromStateKey,
                transition.ToStateKey,
                RequiresApproval: false))
            .ToArray();
    }

    public async Task<RecordWorkflowStateDto> GetRecordWorkflowAsync(
        Guid recordId,
        ClaimsPrincipal principal,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.Records
            .AsNoTracking()
            .Include(candidate => candidate.WorkflowDefinition)
            .Include(candidate => candidate.WorkflowDefinitionVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

        if (record is null)
        {
            throw new RecordWorkflowException(StatusCodes.Status404NotFound, "Record was not found.");
        }

        if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.View, cancellationToken))
        {
            throw new RecordWorkflowException(StatusCodes.Status403Forbidden, "Record access was denied.");
        }

        var canMutateWorkflow = await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.ChangeStatus, cancellationToken);
        return await ToStateDtoAsync(record, canMutateWorkflow, cancellationToken);
    }

    public async Task<RecordWorkflowStateDto> StartRecordWorkflowAsync(
        Guid recordId,
        StartRecordWorkflowRequest request,
        ClaimsPrincipal principal,
        Guid? actorUserId,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (request.WorkflowDefinitionId == Guid.Empty)
        {
            throw new RecordWorkflowException(StatusCodes.Status400BadRequest, "Workflow definition id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp))
        {
            throw new RecordWorkflowException(StatusCodes.Status400BadRequest, "Record concurrency stamp is required.");
        }

        TriggerRecordSnapshot? beforeSnapshot = null;
        TriggerRecordSnapshot? afterSnapshot = null;
        string? previousStatus = null;
        string? currentStatus = null;

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            var record = await dbContext.Records
                .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

            if (record is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status404NotFound, "Record was not found.");
            }

            if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.ChangeStatus, cancellationToken))
            {
                throw new RecordWorkflowException(StatusCodes.Status403Forbidden, "Record access was denied.");
            }

            EnsureConcurrencyStamp(record.ConcurrencyStamp, request.ConcurrencyStamp);

            if (record.WorkflowDefinitionId is not null
                || record.WorkflowDefinitionVersionId is not null
                || !string.IsNullOrWhiteSpace(record.WorkflowStateKey))
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Record already has an active workflow.");
            }

            var workflow = await dbContext.Workflows
                .Include(candidate => candidate.CurrentVersion)
                .FirstOrDefaultAsync(candidate => candidate.Id == request.WorkflowDefinitionId && !candidate.IsDeleted, cancellationToken);

            if (workflow is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status404NotFound, "Workflow was not found.");
            }

            if (workflow.FormId != record.FormId)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow belongs to a different form.");
            }

            if (!workflow.IsEnabled)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow is disabled.");
            }

            if (!string.Equals(workflow.Status, WorkflowDefinitionStatuses.Published, StringComparison.Ordinal)
                || workflow.CurrentVersionId is null
                || workflow.CurrentVersion is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow has no published version.");
            }

            var config = DeserializeConfig(workflow.CurrentVersion.ConfigJson);
            var initialState = config.States.FirstOrDefault(state => string.Equals(state.Key, config.InitialStateKey, StringComparison.Ordinal));
            if (initialState is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow initial state was not found.");
            }

            var values = DeserializeValues(record.ValuesJson);
            beforeSnapshot = ToTriggerSnapshot(record, values);
            previousStatus = record.Status;
            record.WorkflowDefinitionId = workflow.Id;
            record.WorkflowDefinitionVersionId = workflow.CurrentVersion.Id;
            record.WorkflowStateKey = config.InitialStateKey;
            record.Status = config.InitialStateKey;
            record.UpdatedById = actorUserId;
            currentStatus = record.Status;
            afterSnapshot = ToTriggerSnapshot(record, values);

            AddHistory(
                workflow.Id,
                workflow.CurrentVersion.Id,
                record.FormId,
                record.Id,
                null,
                config.InitialStateKey,
                null,
                RecordWorkflowHistoryActions.Started,
                actorUserId,
                new { workflow.Name, workflow.CurrentVersion.VersionNumber });

            AddRecordAudit(
                record.Id,
                "record_workflow_started",
                actorUserId,
                new
                {
                    WorkflowDefinitionId = workflow.Id,
                    WorkflowDefinitionVersionId = workflow.CurrentVersion.Id,
                    StateKey = config.InitialStateKey
                });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await DispatchStatusChangedIfNeededAsync(beforeSnapshot, afterSnapshot, actorUserId, previousStatus, currentStatus, cancellationToken);

        return await GetRecordWorkflowAsync(recordId, principal, permissionService, cancellationToken);
    }

    public async Task<RecordWorkflowStateDto> ExecuteTransitionAsync(
        Guid recordId,
        string transitionKey,
        ExecuteRecordWorkflowTransitionRequest request,
        ClaimsPrincipal principal,
        Guid? actorUserId,
        PermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var normalizedTransitionKey = transitionKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTransitionKey))
        {
            throw new RecordWorkflowException(StatusCodes.Status400BadRequest, "Workflow transition key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp))
        {
            throw new RecordWorkflowException(StatusCodes.Status400BadRequest, "Record concurrency stamp is required.");
        }

        TriggerRecordSnapshot? beforeSnapshot = null;
        TriggerRecordSnapshot? afterSnapshot = null;
        string? previousStatus = null;
        string? currentStatus = null;

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            var record = await dbContext.Records
                .FirstOrDefaultAsync(candidate => candidate.Id == recordId && !candidate.IsDeleted, cancellationToken);

            if (record is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status404NotFound, "Record was not found.");
            }

            if (!await permissionService.CanAccessRecordAsync(principal, record, PlatformPermissions.Form.ChangeStatus, cancellationToken))
            {
                throw new RecordWorkflowException(StatusCodes.Status403Forbidden, "Record access was denied.");
            }

            EnsureConcurrencyStamp(record.ConcurrencyStamp, request.ConcurrencyStamp);

            if (record.WorkflowDefinitionId is null
                || record.WorkflowDefinitionVersionId is null
                || string.IsNullOrWhiteSpace(record.WorkflowStateKey))
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Record has no active workflow.");
            }

            var workflow = await dbContext.Workflows
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == record.WorkflowDefinitionId.Value && !candidate.IsDeleted, cancellationToken);

            if (workflow is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Record workflow definition was not found.");
            }

            if (!workflow.IsEnabled)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow is disabled.");
            }

            var version = await dbContext.WorkflowVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == record.WorkflowDefinitionVersionId.Value, cancellationToken);

            if (version is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Record workflow version was not found.");
            }

            var config = DeserializeConfig(version.ConfigJson);
            var transition = config.Transitions.FirstOrDefault(candidate =>
                string.Equals(candidate.Key, normalizedTransitionKey, StringComparison.Ordinal)
                && string.Equals(candidate.FromStateKey, record.WorkflowStateKey, StringComparison.Ordinal));

            if (transition is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow transition is not available from the current state.");
            }

            if (!string.IsNullOrWhiteSpace(transition.ApprovalStepKey))
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow transition requires approval.");
            }

            var targetState = config.States.FirstOrDefault(state => string.Equals(state.Key, transition.ToStateKey, StringComparison.Ordinal));
            if (targetState is null)
            {
                throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow target state was not found.");
            }

            var values = DeserializeValues(record.ValuesJson);
            beforeSnapshot = ToTriggerSnapshot(record, values);
            previousStatus = record.Status;
            var fromStateKey = record.WorkflowStateKey;
            record.WorkflowStateKey = transition.ToStateKey;
            record.Status = transition.ToStateKey;
            record.UpdatedById = actorUserId;
            currentStatus = record.Status;
            afterSnapshot = ToTriggerSnapshot(record, values);

            AddHistory(
                workflow.Id,
                version.Id,
                record.FormId,
                record.Id,
                fromStateKey,
                transition.ToStateKey,
                transition.Key,
                RecordWorkflowHistoryActions.Transitioned,
                actorUserId,
                new { transition.Name });

            AddRecordAudit(
                record.Id,
                "record_workflow_transitioned",
                actorUserId,
                new
                {
                    WorkflowDefinitionId = workflow.Id,
                    WorkflowDefinitionVersionId = version.Id,
                    FromStateKey = fromStateKey,
                    ToStateKey = transition.ToStateKey,
                    TransitionKey = transition.Key
                });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await DispatchStatusChangedIfNeededAsync(beforeSnapshot, afterSnapshot, actorUserId, previousStatus, currentStatus, cancellationToken);

        return await GetRecordWorkflowAsync(recordId, principal, permissionService, cancellationToken);
    }

    private async Task<RecordWorkflowStateDto> ToStateDtoAsync(
        FormRecord record,
        bool canMutateWorkflow,
        CancellationToken cancellationToken)
    {
        WorkflowDefinition? workflow = record.WorkflowDefinition;
        WorkflowDefinitionVersion? version = record.WorkflowDefinitionVersion;
        IReadOnlyList<RecordWorkflowStartOptionDto> availableWorkflows = Array.Empty<RecordWorkflowStartOptionDto>();
        IReadOnlyList<RecordWorkflowTransitionDto> availableTransitions = Array.Empty<RecordWorkflowTransitionDto>();

        if (record.WorkflowDefinitionId is not null && workflow is null)
        {
            workflow = await dbContext.Workflows
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == record.WorkflowDefinitionId.Value && !candidate.IsDeleted, cancellationToken);
        }

        if (record.WorkflowDefinitionVersionId is not null && version is null)
        {
            version = await dbContext.WorkflowVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == record.WorkflowDefinitionVersionId.Value, cancellationToken);
        }

        if (canMutateWorkflow && workflow?.IsEnabled == true && version is not null && !string.IsNullOrWhiteSpace(record.WorkflowStateKey))
        {
            availableTransitions = GetAvailableDirectTransitions(DeserializeConfig(version.ConfigJson), record.WorkflowStateKey);
        }
        else if (canMutateWorkflow
            && record.WorkflowDefinitionId is null
            && record.WorkflowDefinitionVersionId is null
            && string.IsNullOrWhiteSpace(record.WorkflowStateKey))
        {
            var workflows = await dbContext.Workflows
                .AsNoTracking()
                .Include(candidate => candidate.CurrentVersion)
                .Where(candidate => candidate.FormId == record.FormId && !candidate.IsDeleted)
                .Where(candidate => candidate.IsEnabled)
                .Where(candidate => candidate.Status == WorkflowDefinitionStatuses.Published)
                .Where(candidate => candidate.CurrentVersionId != null && candidate.CurrentVersion != null)
                .OrderBy(candidate => candidate.Name)
                .ToArrayAsync(cancellationToken);

            availableWorkflows = workflows
                .Select(candidate =>
                {
                    var config = DeserializeConfig(candidate.CurrentVersion!.ConfigJson);
                    return new RecordWorkflowStartOptionDto(
                        candidate.Id,
                        candidate.Name,
                        candidate.CurrentVersion.VersionNumber,
                        config.InitialStateKey);
                })
                .ToArray();
        }

        var history = await dbContext.WorkflowHistory
            .AsNoTracking()
            .Where(candidate => candidate.RecordId == record.Id)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.Id)
            .Take(HistoryLimit)
            .Select(candidate => new RecordWorkflowHistoryDto(
                candidate.Id,
                candidate.WorkflowDefinitionId,
                candidate.WorkflowDefinitionVersionId,
                candidate.RecordId,
                candidate.FromStateKey,
                candidate.ToStateKey,
                candidate.TransitionKey,
                candidate.Action,
                candidate.ActorUserId,
                candidate.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new RecordWorkflowStateDto(
            record.Id,
            record.FormId,
            record.WorkflowDefinitionId,
            record.WorkflowDefinitionVersionId,
            workflow?.Name,
            version?.VersionNumber,
            record.WorkflowStateKey,
            availableWorkflows,
            availableTransitions,
            history,
            record.ConcurrencyStamp);
    }

    private async Task DispatchStatusChangedIfNeededAsync(
        TriggerRecordSnapshot? beforeSnapshot,
        TriggerRecordSnapshot? afterSnapshot,
        Guid? actorUserId,
        string? previousStatus,
        string? currentStatus,
        CancellationToken cancellationToken)
    {
        if (beforeSnapshot is null
            || afterSnapshot is null
            || string.Equals(previousStatus, currentStatus, StringComparison.Ordinal))
        {
            return;
        }

        await triggerDispatcher.DispatchAsync(new TriggerEventContext(
            TriggerEvents.StatusChanged,
            afterSnapshot.FormId,
            afterSnapshot.RecordId,
            actorUserId,
            beforeSnapshot,
            afterSnapshot,
            Array.Empty<string>(),
            previousStatus,
            currentStatus,
            beforeSnapshot.AssignedToUserId,
            afterSnapshot.AssignedToUserId,
            beforeSnapshot.AssignedGroupId,
            afterSnapshot.AssignedGroupId,
            DateTimeOffset.UtcNow), cancellationToken);
    }

    private void AddHistory(
        Guid workflowDefinitionId,
        Guid workflowDefinitionVersionId,
        Guid formId,
        Guid recordId,
        string? fromStateKey,
        string toStateKey,
        string? transitionKey,
        string action,
        Guid? actorUserId,
        object? metadata)
    {
        dbContext.WorkflowHistory.Add(new WorkflowHistoryEntry
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowDefinitionVersionId = workflowDefinitionVersionId,
            FormId = formId,
            RecordId = recordId,
            FromStateKey = fromStateKey,
            ToStateKey = toStateKey,
            TransitionKey = transitionKey,
            Action = action,
            ActorUserId = actorUserId,
            CreatedById = actorUserId,
            MetadataJson = metadata is null ? null : Serialize(metadata)
        });
    }

    private void AddRecordAudit(Guid recordId, string action, Guid? actorUserId, object? metadata)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = recordId,
            Action = action,
            UserId = actorUserId,
            MetadataJson = metadata is null ? null : Serialize(metadata)
        });
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string requestedStamp)
    {
        if (!string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new RecordWorkflowException(StatusCodes.Status409Conflict, "The record was changed by another user.");
        }
    }

    private static WorkflowDefinitionConfig DeserializeConfig(JsonDocument configJson)
    {
        var config = configJson.RootElement.Deserialize<WorkflowDefinitionConfig>(JsonOptions);
        return WorkflowDefinitionValidator.NormalizeConfig(config);
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static TriggerRecordSnapshot ToTriggerSnapshot(FormRecord record, IReadOnlyDictionary<string, object?> values)
    {
        return new TriggerRecordSnapshot(
            record.Id,
            record.FormId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            values);
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }
}
