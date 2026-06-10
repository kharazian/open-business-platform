using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerActionRegistry actionRegistry;

    public TriggerExecutionService(OpenBusinessPlatformDbContext dbContext, TriggerActionRegistry actionRegistry)
    {
        this.dbContext = dbContext;
        this.actionRegistry = actionRegistry;
    }

    public async Task ExecuteAsync(
        TriggerDefinition trigger,
        TriggerEventContext context,
        CancellationToken cancellationToken)
    {
        var conditions = DeserializeConditions(trigger.ConditionsJson);

        if (!TriggerConditionEvaluator.Matches(conditions, context))
        {
            return;
        }

        await ExecuteMatchedActionsAsync(trigger, context, null, "Record", context.RecordId, cancellationToken);
    }

    public async Task<TriggerExecutionLogDto> RetryFailedLogAsync(
        Guid triggerId,
        Guid logId,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var trigger = await dbContext.Triggers
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == triggerId && !candidate.IsDeleted, cancellationToken);

        if (trigger is null)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger was not found.");
        }

        if (!trigger.IsEnabled)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Disabled triggers cannot be retried.");
        }

        var sourceLog = await dbContext.TriggerLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == logId && candidate.TriggerId == triggerId, cancellationToken);

        if (sourceLog is null)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger log was not found.");
        }

        if (!string.Equals(sourceLog.Status, TriggerExecutionStatuses.Failed, StringComparison.Ordinal))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Only failed trigger logs can be retried.");
        }

        var sourceContext = DeserializeEventContext(sourceLog.InputJson);
        var context = sourceContext with
        {
            ActorUserId = actorUserId ?? sourceContext.ActorUserId
        };
        var retryLog = await ExecuteMatchedActionsAsync(trigger, context, sourceLog.Id, sourceLog.EntityType, sourceLog.EntityId, cancellationToken);

        return TriggerDefinitionService.ToLogDto(retryLog);
    }

    public async Task<TriggerExecutionLog> ExecuteAutomaticRetryAsync(
        TriggerDefinition trigger,
        TriggerExecutionLog sourceLog,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(sourceLog.Status, TriggerExecutionStatuses.Failed, StringComparison.Ordinal))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Only failed trigger logs can be retried.");
        }

        var context = DeserializeEventContext(sourceLog.InputJson);
        return await ExecuteMatchedActionsAsync(trigger, context, sourceLog.Id, sourceLog.EntityType, sourceLog.EntityId, cancellationToken);
    }

    public async Task<TriggerExecutionLog> ExecuteScheduledAsync(
        TriggerDefinition trigger,
        DateTimeOffset scheduledAt,
        DateTimeOffset? lockedAt,
        DateTimeOffset? nextRunAt,
        CancellationToken cancellationToken)
    {
        if (!TriggerEvents.IsScheduled(trigger.EventName))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Only scheduled triggers can be executed by the schedule worker.");
        }

        var snapshot = new TriggerRecordSnapshot(
            Guid.Empty,
            trigger.FormId,
            "scheduled",
            null,
            null,
            null,
            null,
            new Dictionary<string, object?>());
        var context = new TriggerEventContext(
            trigger.EventName,
            trigger.FormId,
            Guid.Empty,
            null,
            null,
            snapshot,
            Array.Empty<string>(),
            null,
            null,
            null,
            null,
            null,
            null,
            scheduledAt);

        return await ExecuteMatchedActionsAsync(
            trigger,
            context,
            null,
            "Schedule",
            trigger.Id,
            cancellationToken,
            new TriggerScheduledRunMetadata(scheduledAt, lockedAt ?? DateTimeOffset.UtcNow, NextRunAt: nextRunAt));
    }

    public async Task<TriggerExecutionLog> SkipScheduledAsync(
        TriggerDefinition trigger,
        DateTimeOffset scheduledAt,
        DateTimeOffset lockedAt,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!TriggerEvents.IsScheduled(trigger.EventName))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Only scheduled triggers can be skipped by the schedule worker.");
        }

        var now = DateTimeOffset.UtcNow;
        var metadata = new TriggerScheduledRunMetadata(
            scheduledAt,
            lockedAt,
            now,
            null,
            TriggerExecutionStatuses.Skipped,
            reason);
        var log = new TriggerExecutionLog
        {
            Id = Guid.NewGuid(),
            TriggerId = trigger.Id,
            FormId = trigger.FormId,
            EventName = trigger.EventName,
            EntityType = "Schedule",
            EntityId = trigger.Id,
            Status = TriggerExecutionStatuses.Skipped,
            InputJson = Serialize(new { schedule = metadata }),
            ResultJson = Serialize(new { schedule = metadata, actions = Array.Empty<IReadOnlyDictionary<string, object?>>() }),
            ErrorMessage = reason,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now
        };

        dbContext.TriggerLogs.Add(log);
        AddAudit(trigger.Id, "trigger_schedule_skipped", null, log.Id, null, reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        return log;
    }

    private async Task<TriggerExecutionLog> ExecuteMatchedActionsAsync(
        TriggerDefinition trigger,
        TriggerEventContext context,
        Guid? retryOfLogId,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken,
        TriggerScheduledRunMetadata? scheduleMetadata = null)
    {
        var now = DateTimeOffset.UtcNow;
        var log = new TriggerExecutionLog
        {
            Id = Guid.NewGuid(),
            TriggerId = trigger.Id,
            FormId = trigger.FormId,
            EventName = context.EventName,
            EntityType = entityType,
            EntityId = entityId,
            Status = TriggerExecutionStatuses.Success,
            InputJson = SerializeInput(context, retryOfLogId, scheduleMetadata),
            StartedAt = now,
            CreatedAt = now
        };
        var actionResults = new List<IReadOnlyDictionary<string, object?>>();

        dbContext.TriggerLogs.Add(log);

        try
        {
            foreach (var action in DeserializeActions(trigger.ActionsJson))
            {
                try
                {
                    actionResults.Add(await actionRegistry.ExecuteAsync(action, context, trigger.Id, log.Id, cancellationToken));
                }
                catch (Exception actionException)
                {
                    actionResults.Add(BuildFailedActionResult(action, actionException));
                    throw;
                }
            }

            log.Status = TriggerExecutionStatuses.Success;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ResultJson = SerializeResult(
                actionResults,
                retryOfLogId,
                CompleteScheduleMetadata(scheduleMetadata, TriggerExecutionStatuses.Success, log.CompletedAt.Value));
            AddAudit(
                trigger.Id,
                retryOfLogId is null ? "trigger_executed" : "trigger_retry_executed",
                context.ActorUserId,
                log.Id,
                retryOfLogId,
                null);
        }
        catch (Exception exception)
        {
            log.Status = TriggerExecutionStatuses.Failed;
            log.ErrorMessage = exception.Message;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ResultJson = SerializeResult(
                actionResults,
                retryOfLogId,
                CompleteScheduleMetadata(scheduleMetadata, TriggerExecutionStatuses.Failed, log.CompletedAt.Value));

            var retryPolicy = TriggerRetryPolicy.FromTrigger(trigger);

            if (retryOfLogId is null && retryPolicy is not null)
            {
                TriggerRetryScheduler.ScheduleInitialFailure(log, retryPolicy, log.CompletedAt.Value);
            }

            AddAudit(
                trigger.Id,
                retryOfLogId is null ? "trigger_failed" : "trigger_retry_failed",
                context.ActorUserId,
                log.Id,
                retryOfLogId,
                exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return log;
    }

    private void AddAudit(Guid triggerId, string action, Guid? userId, Guid triggerLogId, Guid? retryOfLogId, string? errorMessage)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Trigger",
            EntityId = triggerId,
            Action = action,
            UserId = userId,
            MetadataJson = Serialize(new
            {
                triggerLogId,
                retryOfLogId,
                errorMessage
            })
        });
    }

    private static TriggerConditionGroupDefinition DeserializeConditions(JsonDocument conditionsJson)
    {
        var conditions = conditionsJson.RootElement.Deserialize<TriggerConditionGroupDefinition>(JsonOptions);
        return TriggerDefinitionValidator.NormalizeConditions(conditions);
    }

    private static IReadOnlyList<TriggerActionDefinition> DeserializeActions(JsonDocument actionsJson)
    {
        var actions = actionsJson.RootElement.Deserialize<IReadOnlyList<TriggerActionDefinition>>(JsonOptions);
        return TriggerDefinitionValidator.NormalizeActions(actions);
    }

    private static TriggerEventContext DeserializeEventContext(JsonDocument inputJson)
    {
        try
        {
            return inputJson.RootElement.Deserialize<TriggerEventContext>(JsonOptions)
                ?? throw new TriggerManagementException(StatusCodes.Status409Conflict, "Trigger log input is not available for retry.");
        }
        catch (JsonException exception)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, $"Trigger log input is not available for retry. {exception.Message}");
        }
    }

    private static JsonDocument SerializeInput(
        TriggerEventContext context,
        Guid? retryOfLogId,
        TriggerScheduledRunMetadata? scheduleMetadata = null)
    {
        var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(context, JsonOptions), JsonOptions)
            ?? new Dictionary<string, object?>();

        if (retryOfLogId is not null)
        {
            input["retry"] = new TriggerRetryMetadata(retryOfLogId.Value);
        }

        if (scheduleMetadata is not null)
        {
            input["schedule"] = scheduleMetadata;
        }

        return Serialize(input);
    }

    private static JsonDocument SerializeResult(
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> actionResults,
        Guid? retryOfLogId,
        TriggerScheduledRunMetadata? scheduleMetadata = null)
    {
        if (retryOfLogId is null && scheduleMetadata is null)
        {
            return Serialize(new { actions = actionResults });
        }

        if (retryOfLogId is null)
        {
            return Serialize(new { schedule = scheduleMetadata, actions = actionResults });
        }

        if (scheduleMetadata is null)
        {
            return Serialize(new { retry = new TriggerRetryMetadata(retryOfLogId.Value), actions = actionResults });
        }

        return Serialize(new
        {
            retry = new TriggerRetryMetadata(retryOfLogId.Value),
            schedule = scheduleMetadata,
            actions = actionResults
        });
    }

    private static TriggerScheduledRunMetadata? CompleteScheduleMetadata(
        TriggerScheduledRunMetadata? scheduleMetadata,
        string status,
        DateTimeOffset completedAt)
    {
        return scheduleMetadata is null
            ? null
            : scheduleMetadata with { CompletedAt = completedAt, Status = status };
    }

    private static IReadOnlyDictionary<string, object?> BuildFailedActionResult(TriggerActionDefinition action, Exception exception)
    {
        var result = new Dictionary<string, object?>
        {
            ["actionId"] = action.Id,
            ["type"] = action.Type,
            ["status"] = TriggerWorkflowStartResultStatuses.Failed,
            ["errorMessage"] = exception.Message
        };

        if (string.Equals(action.Type, TriggerActionTypes.StartWorkflow, StringComparison.Ordinal))
        {
            result["workflowStartStatus"] = TriggerWorkflowStartResultStatuses.Failed;
            result["workflowDefinitionId"] = action.WorkflowDefinitionId;
        }

        return result;
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }
}
