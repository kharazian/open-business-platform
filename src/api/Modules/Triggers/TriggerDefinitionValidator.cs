using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerDefinitionValidator
{
    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        CreateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds)
    {
        return Validate(
            schema,
            request.Name,
            request.EventName,
            request.Conditions,
            request.Actions,
            activeUserIds,
            activeGroupIds,
            requireConcurrencyStamp: false,
            concurrencyStamp: null);
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        UpdateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds)
    {
        return Validate(
            schema,
            request.Name,
            request.EventName,
            request.Conditions,
            request.Actions,
            activeUserIds,
            activeGroupIds,
            requireConcurrencyStamp: true,
            concurrencyStamp: request.ConcurrencyStamp);
    }

    public static TriggerConditionGroupDefinition NormalizeConditions(TriggerConditionGroupDefinition? conditions)
    {
        return new TriggerConditionGroupDefinition(
            string.IsNullOrWhiteSpace(conditions?.Mode) ? TriggerConditionModes.All : conditions.Mode.Trim(),
            (conditions?.Conditions ?? Array.Empty<TriggerConditionDefinition>())
                .Select(condition => condition with
                {
                    Type = Normalize(condition.Type),
                    FieldId = NormalizeOptional(condition.FieldId),
                    Status = NormalizeOptional(condition.Status)
                })
                .ToArray());
    }

    public static IReadOnlyList<TriggerActionDefinition> NormalizeActions(IReadOnlyList<TriggerActionDefinition>? actions)
    {
        return (actions ?? Array.Empty<TriggerActionDefinition>())
            .Select(action => action with
            {
                Id = Normalize(action.Id),
                Type = Normalize(action.Type),
                Message = NormalizeOptional(action.Message),
                To = action.To?
                    .Select(Normalize)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Subject = NormalizeOptional(action.Subject),
                Body = NormalizeOptional(action.Body),
                Status = NormalizeOptional(action.Status)
            })
            .ToArray();
    }

    private static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        string? name,
        string? eventName,
        TriggerConditionGroupDefinition? conditions,
        IReadOnlyList<TriggerActionDefinition>? actions,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        bool requireConcurrencyStamp,
        string? concurrencyStamp)
    {
        var errors = new List<TriggerValidationError>();
        var normalizedConditions = NormalizeConditions(conditions);
        var normalizedActions = NormalizeActions(actions);
        var fieldIds = schema.Fields
            .Select(field => field.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(Error("name", "trigger.name.required", "Trigger name is required."));
        }

        if (!TriggerEvents.Supported.Contains(Normalize(eventName)))
        {
            errors.Add(Error("eventName", "trigger.event.unsupported", "Trigger event is not supported."));
        }

        if (requireConcurrencyStamp && string.IsNullOrWhiteSpace(concurrencyStamp))
        {
            errors.Add(Error("concurrencyStamp", "trigger.concurrency.required", "Trigger concurrency stamp is required."));
        }

        ValidateConditions(normalizedConditions, fieldIds, errors);
        ValidateActions(normalizedActions, activeUserIds, activeGroupIds, errors);

        return new TriggerValidationResult(errors);
    }

    private static void ValidateConditions(
        TriggerConditionGroupDefinition group,
        IReadOnlySet<string> fieldIds,
        List<TriggerValidationError> errors)
    {
        if (!string.Equals(group.Mode, TriggerConditionModes.All, StringComparison.Ordinal))
        {
            errors.Add(Error("conditions.mode", "trigger.conditions.mode", "Only all-mode trigger conditions are supported."));
        }

        for (var index = 0; index < group.Conditions.Count; index++)
        {
            var condition = group.Conditions[index];
            var path = $"conditions.conditions[{index}]";

            if (!TriggerConditionTypes.Supported.Contains(condition.Type))
            {
                errors.Add(Error($"{path}.type", "trigger.condition.type", "Trigger condition type is not supported."));
                continue;
            }

            switch (condition.Type)
            {
                case TriggerConditionTypes.FieldEquals:
                    ValidateKnownField(condition.FieldId, fieldIds, $"{path}.fieldId", errors);

                    if (condition.Value is null)
                    {
                        errors.Add(Error($"{path}.value", "trigger.condition.value_required", "Field equality condition value is required."));
                    }

                    break;
                case TriggerConditionTypes.FieldChanged:
                    ValidateKnownField(condition.FieldId, fieldIds, $"{path}.fieldId", errors);
                    break;
                case TriggerConditionTypes.StatusChangedTo:
                    if (string.IsNullOrWhiteSpace(condition.Status))
                    {
                        errors.Add(Error($"{path}.status", "trigger.condition.status_required", "Condition status is required."));
                    }

                    break;
                case TriggerConditionTypes.DepartmentEquals:
                    if (condition.DepartmentId is null || condition.DepartmentId == Guid.Empty)
                    {
                        errors.Add(Error($"{path}.departmentId", "trigger.condition.department_required", "Condition department is required."));
                    }

                    break;
                case TriggerConditionTypes.AssignedToUser:
                    if (condition.UserId is null || condition.UserId == Guid.Empty)
                    {
                        errors.Add(Error($"{path}.userId", "trigger.condition.user_required", "Condition user is required."));
                    }

                    break;
                case TriggerConditionTypes.AssignedToGroup:
                    if (condition.GroupId is null || condition.GroupId == Guid.Empty)
                    {
                        errors.Add(Error($"{path}.groupId", "trigger.condition.group_required", "Condition group is required."));
                    }

                    break;
            }
        }
    }

    private static void ValidateActions(
        IReadOnlyList<TriggerActionDefinition> actions,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        List<TriggerValidationError> errors)
    {
        if (actions.Count == 0)
        {
            errors.Add(Error("actions", "trigger.actions.required", "Add at least one trigger action."));
            return;
        }

        var actionIds = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var path = $"actions[{index}]";

            if (string.IsNullOrWhiteSpace(action.Id))
            {
                errors.Add(Error($"{path}.id", "trigger.action.id_required", "Action id is required."));
            }
            else if (!actionIds.Add(action.Id))
            {
                errors.Add(Error($"{path}.id", "trigger.action.id_duplicate", "Action ids must be unique."));
            }

            if (!TriggerActionTypes.Supported.Contains(action.Type))
            {
                errors.Add(Error($"{path}.type", "trigger.action.type", "Trigger action type is not supported."));
                continue;
            }

            switch (action.Type)
            {
                case TriggerActionTypes.WriteAuditEntry:
                    if (string.IsNullOrWhiteSpace(action.Message))
                    {
                        errors.Add(Error($"{path}.message", "trigger.action.message_required", "Audit action message is required."));
                    }

                    break;
                case TriggerActionTypes.SendEmail:
                    if (action.To is null || action.To.Count == 0)
                    {
                        errors.Add(Error($"{path}.to", "trigger.action.email_to_required", "Email action requires at least one recipient."));
                    }

                    if (string.IsNullOrWhiteSpace(action.Subject))
                    {
                        errors.Add(Error($"{path}.subject", "trigger.action.email_subject_required", "Email action subject is required."));
                    }

                    break;
                case TriggerActionTypes.ChangeStatus:
                    if (string.IsNullOrWhiteSpace(action.Status))
                    {
                        errors.Add(Error($"{path}.status", "trigger.action.status_required", "Status action requires a status."));
                    }

                    break;
                case TriggerActionTypes.AssignRecord:
                    ValidateAssignAction(action, activeUserIds, activeGroupIds, path, errors);
                    break;
            }
        }
    }

    private static void ValidateAssignAction(
        TriggerActionDefinition action,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        string path,
        List<TriggerValidationError> errors)
    {
        var hasUser = action.AssignedToUserId is not null && action.AssignedToUserId != Guid.Empty;
        var hasGroup = action.AssignedGroupId is not null && action.AssignedGroupId != Guid.Empty;

        if (hasUser == hasGroup)
        {
            errors.Add(Error($"{path}.assignment", "trigger.action.assignment_target", "Assign action requires exactly one user or group target."));
            return;
        }

        if (hasUser && !activeUserIds.Contains(action.AssignedToUserId!.Value))
        {
            errors.Add(Error($"{path}.assignedToUserId", "trigger.action.user_missing", "Assigned user is not active or was not found."));
        }

        if (hasGroup && !activeGroupIds.Contains(action.AssignedGroupId!.Value))
        {
            errors.Add(Error($"{path}.assignedGroupId", "trigger.action.group_missing", "Assigned group is not active or was not found."));
        }
    }

    private static void ValidateKnownField(
        string? fieldId,
        IReadOnlySet<string> fieldIds,
        string path,
        List<TriggerValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            errors.Add(Error(path, "trigger.condition.field_required", "Condition field is required."));
            return;
        }

        if (!fieldIds.Contains(fieldId))
        {
            errors.Add(Error(path, "trigger.condition.field_unknown", "Condition field does not exist on this form."));
        }
    }

    private static TriggerValidationError Error(string path, string code, string message)
    {
        return new TriggerValidationError(path, code, message);
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
