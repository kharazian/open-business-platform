using System.Text.Json;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerConditionEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool Matches(TriggerConditionGroupDefinition? group, TriggerEventContext context)
    {
        var normalizedGroup = TriggerDefinitionValidator.NormalizeConditions(group);

        if (normalizedGroup.Conditions.Count == 0)
        {
            return true;
        }

        if (!string.Equals(normalizedGroup.Mode, TriggerConditionModes.All, StringComparison.Ordinal))
        {
            return false;
        }

        return normalizedGroup.Conditions.All(condition => Matches(condition, context));
    }

    private static bool Matches(TriggerConditionDefinition condition, TriggerEventContext context)
    {
        return condition.Type switch
        {
            TriggerConditionTypes.FieldEquals => FieldEquals(condition, context.After),
            TriggerConditionTypes.FieldChanged => FieldChanged(condition, context),
            TriggerConditionTypes.StatusChangedTo => StringEquals(context.CurrentStatus ?? context.After.Status, condition.Status),
            TriggerConditionTypes.DepartmentEquals => context.After.DepartmentId == condition.DepartmentId,
            TriggerConditionTypes.AssignedToUser => context.After.AssignedToUserId == condition.UserId,
            TriggerConditionTypes.AssignedToGroup => context.After.AssignedGroupId == condition.GroupId,
            _ => false
        };
    }

    private static bool FieldEquals(TriggerConditionDefinition condition, TriggerRecordSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(condition.FieldId)
            || !snapshot.Values.TryGetValue(condition.FieldId, out var value))
        {
            return false;
        }

        return string.Equals(ToComparableJson(value), ToComparableJson(condition.Value), StringComparison.Ordinal);
    }

    private static bool FieldChanged(TriggerConditionDefinition condition, TriggerEventContext context)
    {
        return !string.IsNullOrWhiteSpace(condition.FieldId)
            && context.ChangedFieldIds.Contains(condition.FieldId, StringComparer.Ordinal);
    }

    private static bool StringEquals(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    private static string ToComparableJson(object? value)
    {
        return value switch
        {
            null => "null",
            JsonElement element => element.GetRawText(),
            JsonDocument document => document.RootElement.GetRawText(),
            _ => JsonSerializer.Serialize(value, JsonOptions)
        };
    }
}
