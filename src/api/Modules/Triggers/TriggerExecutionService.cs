using System.Text.Json;
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
            InputJson = Serialize(context),
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
            log.ResultJson = Serialize(new { actions = actionResults });
            log.CompletedAt = DateTimeOffset.UtcNow;
            AddAudit(trigger.Id, "trigger_executed", context.ActorUserId, log.Id, null);
        }
        catch (Exception exception)
        {
            log.Status = TriggerExecutionStatuses.Failed;
            log.ErrorMessage = exception.Message;
            log.ResultJson = Serialize(new { actions = actionResults });
            log.CompletedAt = DateTimeOffset.UtcNow;
            AddAudit(trigger.Id, "trigger_failed", context.ActorUserId, log.Id, exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(Guid triggerId, string action, Guid? userId, Guid triggerLogId, string? errorMessage)
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

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }
}
