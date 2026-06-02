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

        await ExecuteMatchedActionsAsync(trigger, context, null, cancellationToken);
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
        var retryLog = await ExecuteMatchedActionsAsync(trigger, context, sourceLog.Id, cancellationToken);

        return TriggerDefinitionService.ToLogDto(retryLog);
    }

    private async Task<TriggerExecutionLog> ExecuteMatchedActionsAsync(
        TriggerDefinition trigger,
        TriggerEventContext context,
        Guid? retryOfLogId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var log = new TriggerExecutionLog
        {
            Id = Guid.NewGuid(),
            TriggerId = trigger.Id,
            FormId = trigger.FormId,
            EventName = context.EventName,
            EntityType = "Record",
            EntityId = context.RecordId,
            Status = TriggerExecutionStatuses.Success,
            InputJson = SerializeInput(context, retryOfLogId),
            StartedAt = now,
            CreatedAt = now
        };
        var actionResults = new List<IReadOnlyDictionary<string, object?>>();

        dbContext.TriggerLogs.Add(log);

        try
        {
            foreach (var action in DeserializeActions(trigger.ActionsJson))
            {
                actionResults.Add(await actionRegistry.ExecuteAsync(action, context, trigger.Id, log.Id, cancellationToken));
            }

            log.Status = TriggerExecutionStatuses.Success;
            log.ResultJson = SerializeResult(actionResults, retryOfLogId);
            log.CompletedAt = DateTimeOffset.UtcNow;
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
            log.ResultJson = SerializeResult(actionResults, retryOfLogId);
            log.CompletedAt = DateTimeOffset.UtcNow;
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

    private static JsonDocument SerializeInput(TriggerEventContext context, Guid? retryOfLogId)
    {
        if (retryOfLogId is null)
        {
            return Serialize(context);
        }

        var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(context, JsonOptions), JsonOptions)
            ?? new Dictionary<string, object?>();
        input["retry"] = new TriggerRetryMetadata(retryOfLogId.Value);

        return Serialize(input);
    }

    private static JsonDocument SerializeResult(
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> actionResults,
        Guid? retryOfLogId)
    {
        return retryOfLogId is null
            ? Serialize(new { actions = actionResults })
            : Serialize(new { retry = new TriggerRetryMetadata(retryOfLogId.Value), actions = actionResults });
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }
}
