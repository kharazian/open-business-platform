using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Triggers;

namespace OpenBusinessPlatform.Api.Modules.Workflows;

public sealed class WorkflowApprovalService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerEventDispatcher triggerDispatcher;
    private readonly WorkflowActionExecutionService actionExecutionService;

    public WorkflowApprovalService(
        OpenBusinessPlatformDbContext dbContext,
        TriggerEventDispatcher triggerDispatcher,
        WorkflowActionExecutionService actionExecutionService)
    {
        this.dbContext = dbContext;
        this.triggerDispatcher = triggerDispatcher;
        this.actionExecutionService = actionExecutionService;
    }

    public static bool IsApprovalComplete(string mode, IReadOnlyCollection<string> statuses)
    {
        return mode switch
        {
            WorkflowApprovalModes.Any => statuses.Any(status => string.Equals(status, WorkflowApprovalTaskStatuses.Approved, StringComparison.Ordinal)),
            WorkflowApprovalModes.All => statuses.Count > 0 && statuses.All(status => string.Equals(status, WorkflowApprovalTaskStatuses.Approved, StringComparison.Ordinal)),
            _ => false
        };
    }

    public async Task<IReadOnlyCollection<WorkflowApprovalTaskDto>> ListForCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tasks = await dbContext.WorkflowApprovalTasks
            .AsNoTracking()
            .Where(task => task.AssignedToUserId == userId)
            .OrderBy(task => task.Status == WorkflowApprovalTaskStatuses.Pending ? 0 : 1)
            .ThenByDescending(task => task.CreatedAt)
            .ThenByDescending(task => task.Id)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        return tasks.Select(ToDto).ToArray();
    }

    public async Task RequestApprovalAsync(
        FormRecord record,
        WorkflowDefinition workflow,
        WorkflowDefinitionVersion version,
        WorkflowTransitionDefinition transition,
        WorkflowApprovalStepDefinition approvalStep,
        Guid? requestedById,
        CancellationToken cancellationToken)
    {
        var duplicatePending = await dbContext.WorkflowApprovalTasks.AnyAsync(task =>
            task.RecordId == record.Id
            && task.WorkflowDefinitionVersionId == version.Id
            && task.TransitionKey == transition.Key
            && task.FromStateKey == record.WorkflowStateKey
            && task.Status == WorkflowApprovalTaskStatuses.Pending,
            cancellationToken);

        if (duplicatePending)
        {
            throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow approval is already pending.");
        }

        var assignedUserIds = await ResolveAssignedUserIdsAsync(record, approvalStep, cancellationToken);
        if (assignedUserIds.Count == 0)
        {
            throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow approval has no active approvers.");
        }

        var groupId = Guid.NewGuid();
        var tasks = assignedUserIds.Select(userId => new WorkflowApprovalTask
        {
            Id = Guid.NewGuid(),
            ApprovalGroupId = groupId,
            WorkflowDefinitionId = workflow.Id,
            WorkflowDefinitionVersionId = version.Id,
            FormId = record.FormId,
            RecordId = record.Id,
            ApprovalStepKey = approvalStep.Key,
            ApprovalStepName = approvalStep.Name,
            Mode = approvalStep.Mode,
            TransitionKey = transition.Key,
            TransitionName = transition.Name,
            FromStateKey = transition.FromStateKey,
            ToStateKey = transition.ToStateKey,
            Status = WorkflowApprovalTaskStatuses.Pending,
            AssignedToUserId = userId,
            RequestedById = requestedById,
            CreatedById = requestedById
        }).ToArray();

        dbContext.WorkflowApprovalTasks.AddRange(tasks);
        AddHistory(workflow.Id, version.Id, record.FormId, record.Id, transition.FromStateKey, transition.ToStateKey, transition.Key, RecordWorkflowHistoryActions.ApprovalRequested, requestedById, new { approvalStep.Key, approvalStep.Name, approvalStep.Mode, ApprovalGroupId = groupId });
        AddRecordAudit(record.Id, "record_workflow_approval_requested", requestedById, new { ApprovalGroupId = groupId, TransitionKey = transition.Key, ApprovalStepKey = approvalStep.Key });

        await AddNotificationsAsync(tasks, "Workflow approval requested", $"Approval requested for transition '{transition.Name}'.", cancellationToken);
    }

    public async Task<WorkflowApprovalTaskDto> ApproveAsync(Guid approvalTaskId, Guid userId, RespondWorkflowApprovalRequest request, CancellationToken cancellationToken)
    {
        return await RespondAsync(approvalTaskId, userId, request, approved: true, cancellationToken);
    }

    public async Task<WorkflowApprovalTaskDto> RejectAsync(Guid approvalTaskId, Guid userId, RespondWorkflowApprovalRequest request, CancellationToken cancellationToken)
    {
        return await RespondAsync(approvalTaskId, userId, request, approved: false, cancellationToken);
    }

    private async Task<WorkflowApprovalTaskDto> RespondAsync(
        Guid approvalTaskId,
        Guid userId,
        RespondWorkflowApprovalRequest request,
        bool approved,
        CancellationToken cancellationToken)
    {
        TriggerRecordSnapshot? beforeSnapshot = null;
        TriggerRecordSnapshot? afterSnapshot = null;
        string? previousStatus = null;
        string? currentStatus = null;
        WorkflowApprovalTask? responseTask;
        WorkflowTransitionActionContext? failedActionContext = null;

        try
        {
            await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
            {
                responseTask = await dbContext.WorkflowApprovalTasks
                    .FirstOrDefaultAsync(task => task.Id == approvalTaskId && task.AssignedToUserId == userId, cancellationToken);

                if (responseTask is null)
                {
                    throw new RecordWorkflowException(StatusCodes.Status404NotFound, "Workflow approval task was not found.");
                }

                if (!string.Equals(responseTask.Status, WorkflowApprovalTaskStatuses.Pending, StringComparison.Ordinal))
                {
                    throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow approval task is no longer pending.");
                }

                var now = DateTimeOffset.UtcNow;
                responseTask.Status = approved ? WorkflowApprovalTaskStatuses.Approved : WorkflowApprovalTaskStatuses.Rejected;
                responseTask.RespondedById = userId;
                responseTask.RespondedAt = now;
                responseTask.Comment = NormalizeComment(request.Comment);
                responseTask.UpdatedById = userId;

                var groupTasks = await dbContext.WorkflowApprovalTasks
                    .Where(task => task.ApprovalGroupId == responseTask.ApprovalGroupId)
                    .ToArrayAsync(cancellationToken);

                AddHistory(responseTask.WorkflowDefinitionId, responseTask.WorkflowDefinitionVersionId, responseTask.FormId, responseTask.RecordId, responseTask.FromStateKey, responseTask.ToStateKey, responseTask.TransitionKey, approved ? RecordWorkflowHistoryActions.ApprovalApproved : RecordWorkflowHistoryActions.ApprovalRejected, userId, new { responseTask.ApprovalStepKey, responseTask.Comment });

                if (!approved)
                {
                    CancelPendingSiblings(groupTasks, responseTask.Id, userId, now);
                    AddRecordAudit(responseTask.RecordId, "record_workflow_approval_rejected", userId, new { responseTask.ApprovalGroupId, responseTask.TransitionKey });
                    await NotifyRequesterAsync(responseTask, "Workflow approval rejected", $"Transition '{responseTask.TransitionName}' was rejected.", cancellationToken);
                }
                else if (IsApprovalComplete(responseTask.Mode, groupTasks.Select(task => task.Status).ToArray()))
                {
                    var record = await dbContext.Records
                        .FirstOrDefaultAsync(candidate => candidate.Id == responseTask.RecordId && !candidate.IsDeleted, cancellationToken);

                    if (record is null)
                    {
                        throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Approval record was not found.");
                    }

                    if (record.WorkflowDefinitionVersionId != responseTask.WorkflowDefinitionVersionId
                        || !string.Equals(record.WorkflowStateKey, responseTask.FromStateKey, StringComparison.Ordinal))
                    {
                        throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Record workflow state changed before approval completed.");
                    }

                    var version = await dbContext.WorkflowVersions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(candidate => candidate.Id == responseTask.WorkflowDefinitionVersionId, cancellationToken);

                    if (version is null)
                    {
                        throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Record workflow version was not found.");
                    }

                    var config = DeserializeConfig(version.ConfigJson);
                    var transition = config.Transitions.FirstOrDefault(candidate =>
                        string.Equals(candidate.Key, responseTask.TransitionKey, StringComparison.Ordinal)
                        && string.Equals(candidate.FromStateKey, responseTask.FromStateKey, StringComparison.Ordinal)
                        && string.Equals(candidate.ToStateKey, responseTask.ToStateKey, StringComparison.Ordinal));

                    if (transition is null)
                    {
                        throw new RecordWorkflowException(StatusCodes.Status409Conflict, "Workflow transition was not found in the published version.");
                    }

                    var values = DeserializeValues(record.ValuesJson);
                    beforeSnapshot = ToTriggerSnapshot(record, values);
                    previousStatus = record.Status;
                    record.WorkflowStateKey = responseTask.ToStateKey;
                    record.Status = responseTask.ToStateKey;
                    record.UpdatedById = userId;
                    currentStatus = record.Status;
                    afterSnapshot = ToTriggerSnapshot(record, values);
                    failedActionContext = new WorkflowTransitionActionContext(
                        responseTask.WorkflowDefinitionId,
                        responseTask.WorkflowDefinitionVersionId,
                        responseTask.FormId,
                        responseTask.RecordId,
                        responseTask.FromStateKey,
                        responseTask.ToStateKey,
                        responseTask.TransitionKey,
                        responseTask.TransitionName,
                        userId);

                    CancelPendingSiblings(groupTasks, responseTask.Id, userId, now);
                    AddHistory(responseTask.WorkflowDefinitionId, responseTask.WorkflowDefinitionVersionId, responseTask.FormId, responseTask.RecordId, responseTask.FromStateKey, responseTask.ToStateKey, responseTask.TransitionKey, RecordWorkflowHistoryActions.Transitioned, userId, new { responseTask.TransitionName, ApprovedById = userId });
                    AddRecordAudit(responseTask.RecordId, "record_workflow_approval_completed", userId, new { responseTask.ApprovalGroupId, responseTask.TransitionKey });
                    await NotifyRequesterAsync(responseTask, "Workflow approval approved", $"Transition '{responseTask.TransitionName}' was approved.", cancellationToken);
                    await actionExecutionService.ExecuteTransitionActionsAsync(record, transition, failedActionContext, cancellationToken);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (WorkflowActionExecutionException exception)
        {
            dbContext.ChangeTracker.Clear();

            if (failedActionContext is not null)
            {
                await actionExecutionService.PersistRolledBackActionFailureAsync(failedActionContext, exception.Results, cancellationToken);
            }

            throw new RecordWorkflowException(StatusCodes.Status409Conflict, exception.Message);
        }

        await DispatchStatusChangedIfNeededAsync(beforeSnapshot, afterSnapshot, userId, previousStatus, currentStatus, cancellationToken);

        return ToDto(responseTask);
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveAssignedUserIdsAsync(
        FormRecord record,
        WorkflowApprovalStepDefinition approvalStep,
        CancellationToken cancellationToken)
    {
        var assigned = new HashSet<Guid>();

        foreach (var rule in approvalStep.AssigneeRules)
        {
            switch (rule.Type)
            {
                case WorkflowAssigneeRuleTypes.User when rule.UserId is not null:
                    if (await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == rule.UserId.Value && user.IsActive, cancellationToken))
                    {
                        assigned.Add(rule.UserId.Value);
                    }

                    break;
                case WorkflowAssigneeRuleTypes.Group when rule.GroupId is not null:
                    var groupUsers = await dbContext.UserGroups
                        .AsNoTracking()
                        .Where(userGroup => userGroup.GroupId == rule.GroupId.Value && userGroup.User != null && userGroup.User.IsActive)
                        .Select(userGroup => userGroup.UserId)
                        .ToArrayAsync(cancellationToken);

                    foreach (var userId in groupUsers)
                    {
                        assigned.Add(userId);
                    }

                    break;
                case WorkflowAssigneeRuleTypes.DepartmentManager when rule.DepartmentId is not null:
                    var managerId = await dbContext.Departments
                        .AsNoTracking()
                        .Where(department => department.Id == rule.DepartmentId.Value && department.IsActive)
                        .Select(department => department.ManagerUserId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (managerId is not null
                        && await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == managerId.Value && user.IsActive, cancellationToken))
                    {
                        assigned.Add(managerId.Value);
                    }

                    break;
                case WorkflowAssigneeRuleTypes.RecordOwner when record.OwnerId is not null:
                    if (await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == record.OwnerId.Value && user.IsActive, cancellationToken))
                    {
                        assigned.Add(record.OwnerId.Value);
                    }

                    break;
            }
        }

        return assigned.ToArray();
    }

    private async Task AddNotificationsAsync(
        IReadOnlyCollection<WorkflowApprovalTask> tasks,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var recipientIds = tasks.Select(task => task.AssignedToUserId).Distinct().ToArray();
        var disabledRecipientIds = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(preference => recipientIds.Contains(preference.UserId) && !preference.InAppEnabled)
            .Select(preference => preference.UserId)
            .ToArrayAsync(cancellationToken);
        var disabled = disabledRecipientIds.ToHashSet();

        foreach (var task in tasks.Where(task => !disabled.Contains(task.AssignedToUserId)))
        {
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = task.AssignedToUserId,
                Title = title,
                Body = body,
                SourceType = "WorkflowApproval",
                SourceId = task.Id,
                MetadataJson = Serialize(new { task.RecordId, task.WorkflowDefinitionId, task.WorkflowDefinitionVersionId, task.TransitionKey, task.ApprovalStepKey })
            });
        }
    }

    private async Task NotifyRequesterAsync(
        WorkflowApprovalTask task,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        if (task.RequestedById is null)
        {
            return;
        }

        var disabled = await dbContext.NotificationPreferences
            .AsNoTracking()
            .AnyAsync(preference => preference.UserId == task.RequestedById.Value && !preference.InAppEnabled, cancellationToken);

        if (disabled)
        {
            return;
        }

        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = task.RequestedById.Value,
            Title = title,
            Body = body,
            SourceType = "WorkflowApproval",
            SourceId = task.Id,
            MetadataJson = Serialize(new { task.RecordId, task.WorkflowDefinitionId, task.WorkflowDefinitionVersionId, task.TransitionKey, task.ApprovalStepKey })
        });
    }

    private void CancelPendingSiblings(IReadOnlyCollection<WorkflowApprovalTask> groupTasks, Guid taskId, Guid userId, DateTimeOffset now)
    {
        foreach (var task in groupTasks.Where(task => task.Id != taskId && task.Status == WorkflowApprovalTaskStatuses.Pending))
        {
            task.Status = WorkflowApprovalTaskStatuses.Canceled;
            task.RespondedById = userId;
            task.RespondedAt = now;
            task.UpdatedById = userId;
            AddHistory(task.WorkflowDefinitionId, task.WorkflowDefinitionVersionId, task.FormId, task.RecordId, task.FromStateKey, task.ToStateKey, task.TransitionKey, RecordWorkflowHistoryActions.ApprovalCanceled, userId, new { task.ApprovalStepKey, task.ApprovalGroupId });
        }
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

    private void AddHistory(Guid workflowDefinitionId, Guid workflowDefinitionVersionId, Guid formId, Guid recordId, string? fromStateKey, string toStateKey, string? transitionKey, string action, Guid? actorUserId, object? metadata)
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

    private static WorkflowApprovalTaskDto ToDto(WorkflowApprovalTask task)
    {
        return new WorkflowApprovalTaskDto(
            task.Id,
            task.ApprovalGroupId,
            task.WorkflowDefinitionId,
            task.WorkflowDefinitionVersionId,
            task.FormId,
            task.RecordId,
            task.ApprovalStepKey,
            task.ApprovalStepName,
            task.Mode,
            task.TransitionKey,
            task.TransitionName,
            task.FromStateKey,
            task.ToStateKey,
            task.Status,
            task.AssignedToUserId,
            task.RequestedById,
            task.RespondedById,
            task.RespondedAt,
            task.Comment,
            task.CreatedAt);
    }

    private static string? NormalizeComment(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.Length > 1000 ? normalized[..1000] : normalized;
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static WorkflowDefinitionConfig DeserializeConfig(JsonDocument configJson)
    {
        var config = configJson.RootElement.Deserialize<WorkflowDefinitionConfig>(JsonOptions);
        return WorkflowDefinitionValidator.NormalizeConfig(config);
    }

    private static TriggerRecordSnapshot ToTriggerSnapshot(FormRecord record, IReadOnlyDictionary<string, object?> values)
    {
        return new TriggerRecordSnapshot(record.Id, record.FormId, record.Status, record.OwnerId, record.DepartmentId, record.AssignedToUserId, record.AssignedGroupId, values);
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }
}
