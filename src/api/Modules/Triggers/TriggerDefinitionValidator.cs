using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Printing;
using OpenBusinessPlatform.Api.Modules.Workflows;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerDefinitionValidator
{
    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        CreateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds)
    {
        return Validate(schema, request, activeUserIds, activeGroupIds, Array.Empty<TriggerTargetFormSchema>());
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        CreateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms)
    {
        return Validate(
            schema,
            request.Name,
            request.EventName,
            request.Conditions,
            request.Actions,
            request.RetryPolicy,
            request.Schedule,
            activeUserIds,
            activeGroupIds,
            targetForms,
            Array.Empty<TriggerWorkflowStartTarget>(),
            Array.Empty<TriggerPrintTemplateTarget>(),
            sourceFormId: null,
            requireConcurrencyStamp: false,
            concurrencyStamp: null);
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        CreateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        Guid sourceFormId)
    {
        return Validate(schema, request, activeUserIds, activeGroupIds, targetForms, workflowStartTargets, Array.Empty<TriggerPrintTemplateTarget>(), sourceFormId);
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        CreateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        IReadOnlyCollection<TriggerPrintTemplateTarget> printTemplateTargets,
        Guid sourceFormId)
    {
        return Validate(
            schema,
            request.Name,
            request.EventName,
            request.Conditions,
            request.Actions,
            request.RetryPolicy,
            request.Schedule,
            activeUserIds,
            activeGroupIds,
            targetForms,
            workflowStartTargets,
            printTemplateTargets,
            sourceFormId,
            requireConcurrencyStamp: false,
            concurrencyStamp: null);
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        UpdateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds)
    {
        return Validate(schema, request, activeUserIds, activeGroupIds, Array.Empty<TriggerTargetFormSchema>());
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        UpdateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms)
    {
        return Validate(
            schema,
            request.Name,
            request.EventName,
            request.Conditions,
            request.Actions,
            request.RetryPolicy,
            request.Schedule,
            activeUserIds,
            activeGroupIds,
            targetForms,
            Array.Empty<TriggerWorkflowStartTarget>(),
            Array.Empty<TriggerPrintTemplateTarget>(),
            sourceFormId: null,
            requireConcurrencyStamp: true,
            concurrencyStamp: request.ConcurrencyStamp);
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        UpdateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        Guid sourceFormId)
    {
        return Validate(schema, request, activeUserIds, activeGroupIds, targetForms, workflowStartTargets, Array.Empty<TriggerPrintTemplateTarget>(), sourceFormId);
    }

    public static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        UpdateTriggerRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        IReadOnlyCollection<TriggerPrintTemplateTarget> printTemplateTargets,
        Guid sourceFormId)
    {
        return Validate(
            schema,
            request.Name,
            request.EventName,
            request.Conditions,
            request.Actions,
            request.RetryPolicy,
            request.Schedule,
            activeUserIds,
            activeGroupIds,
            targetForms,
            workflowStartTargets,
            printTemplateTargets,
            sourceFormId,
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
                Status = NormalizeOptional(action.Status),
                FieldId = NormalizeOptional(action.FieldId),
                Value = NormalizeActionValue(action.Value),
                Title = NormalizeOptional(action.Title),
                RecipientUserIds = NormalizeIds(action.RecipientUserIds),
                RecipientGroupIds = NormalizeIds(action.RecipientGroupIds),
                Values = NormalizeActionValues(action.Values),
                WebhookUrl = NormalizeOptional(action.WebhookUrl),
                WebhookMethod = NormalizeWebhookMethod(action.WebhookMethod),
                WebhookHeaders = NormalizeWebhookHeaders(action.WebhookHeaders),
                WebhookBody = NormalizeActionValue(action.WebhookBody),
                PrintTemplateId = NormalizeNullableId(action.PrintTemplateId)
            })
            .ToArray();
    }

    public static TriggerRetryPolicyDefinition NormalizeRetryPolicy(TriggerRetryPolicyDefinition? policy)
    {
        return policy is null
            ? new TriggerRetryPolicyDefinition()
            : new TriggerRetryPolicyDefinition(
                policy.IsEnabled,
                Math.Max(0, policy.MaxAttempts),
                Math.Max(0, policy.DelaySeconds));
    }

    public static TriggerScheduleDefinition? NormalizeSchedule(TriggerScheduleDefinition? schedule)
    {
        if (schedule is null)
        {
            return null;
        }

        return new TriggerScheduleDefinition(
            Normalize(schedule.Kind),
            string.IsNullOrWhiteSpace(schedule.TimeZone) ? "Etc/UTC" : schedule.TimeZone.Trim(),
            schedule.StartAt.ToUniversalTime());
    }

    private static TriggerValidationResult Validate(
        FormSchemaDefinition schema,
        string? name,
        string? eventName,
        TriggerConditionGroupDefinition? conditions,
        IReadOnlyList<TriggerActionDefinition>? actions,
        TriggerRetryPolicyDefinition? retryPolicy,
        TriggerScheduleDefinition? schedule,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        IReadOnlyCollection<TriggerPrintTemplateTarget> printTemplateTargets,
        Guid? sourceFormId,
        bool requireConcurrencyStamp,
        string? concurrencyStamp)
    {
        var errors = new List<TriggerValidationError>();
        var normalizedConditions = NormalizeConditions(conditions);
        var normalizedActions = NormalizeActions(actions);
        var normalizedRetryPolicy = NormalizeRetryPolicy(retryPolicy);
        var normalizedSchedule = NormalizeSchedule(schedule);
        var normalizedEventName = Normalize(eventName);
        var sourceFieldsById = schema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var fieldIds = sourceFieldsById.Keys.ToHashSet(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(Error("name", "trigger.name.required", "Trigger name is required."));
        }

        if (!TriggerEvents.Supported.Contains(normalizedEventName))
        {
            errors.Add(Error("eventName", "trigger.event.unsupported", "Trigger event is not supported."));
        }

        if (requireConcurrencyStamp && string.IsNullOrWhiteSpace(concurrencyStamp))
        {
            errors.Add(Error("concurrencyStamp", "trigger.concurrency.required", "Trigger concurrency stamp is required."));
        }

        ValidateRetryPolicy(normalizedRetryPolicy, errors);
        ValidateSchedule(normalizedEventName, normalizedSchedule, normalizedConditions, normalizedActions, errors);
        ValidateConditions(normalizedConditions, fieldIds, errors);
        ValidateActions(normalizedActions, sourceFieldsById, activeUserIds, activeGroupIds, targetForms, workflowStartTargets, printTemplateTargets, sourceFormId, errors);

        return new TriggerValidationResult(errors);
    }

    private static void ValidateRetryPolicy(
        TriggerRetryPolicyDefinition retryPolicy,
        List<TriggerValidationError> errors)
    {
        if (!retryPolicy.IsEnabled)
        {
            return;
        }

        if (retryPolicy.MaxAttempts is < 1 or > 10)
        {
            errors.Add(Error("retryPolicy.maxAttempts", "trigger.retry.max_attempts", "Automatic retry attempts must be between 1 and 10."));
        }

        if (retryPolicy.DelaySeconds is < 30 or > 86400)
        {
            errors.Add(Error("retryPolicy.delaySeconds", "trigger.retry.delay", "Automatic retry delay must be between 30 seconds and 24 hours."));
        }
    }

    private static void ValidateSchedule(
        string eventName,
        TriggerScheduleDefinition? schedule,
        TriggerConditionGroupDefinition conditions,
        IReadOnlyList<TriggerActionDefinition> actions,
        List<TriggerValidationError> errors)
    {
        var scheduledEvent = TriggerEvents.IsScheduled(eventName);

        if (!scheduledEvent)
        {
            if (schedule is not null)
            {
                errors.Add(Error("schedule", "trigger.schedule.unexpected", "Schedule metadata is only supported for scheduled trigger events."));
            }

            return;
        }

        if (schedule is null)
        {
            errors.Add(Error("schedule", "trigger.schedule.required", "Scheduled trigger events require schedule metadata."));
            return;
        }

        if (!TriggerScheduleKinds.Supported.Contains(schedule.Kind))
        {
            errors.Add(Error("schedule.kind", "trigger.schedule.kind", "Schedule kind is not supported."));
        }
        else if (!string.Equals(schedule.Kind, TriggerScheduleKinds.FromEventName(eventName), StringComparison.Ordinal))
        {
            errors.Add(Error("schedule.kind", "trigger.schedule.event_kind", "Schedule kind must match the scheduled trigger event."));
        }

        if (string.IsNullOrWhiteSpace(schedule.TimeZone))
        {
            errors.Add(Error("schedule.timeZone", "trigger.schedule.time_zone", "Schedule time zone is required."));
        }

        if (conditions.Conditions.Count > 0)
        {
            errors.Add(Error("conditions", "trigger.schedule.conditions", "Scheduled triggers do not support record conditions in V4."));
        }

        for (var index = 0; index < actions.Count; index++)
        {
            if (!TriggerActionTypes.ScheduledSupported.Contains(actions[index].Type))
            {
                errors.Add(Error($"actions[{index}].type", "trigger.schedule.action_type", "Scheduled triggers only support email and webhook actions in V4."));
            }

            if (actions[index].PrintTemplateId is not null)
            {
                errors.Add(Error($"actions[{index}].printTemplateId", "trigger.schedule.pdf_attachment", "Scheduled email PDF attachments require a record context."));
            }
        }
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
        IReadOnlyDictionary<string, FormFieldDefinition> sourceFieldsById,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        IReadOnlyCollection<TriggerPrintTemplateTarget> printTemplateTargets,
        Guid? sourceFormId,
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

            var fieldIds = sourceFieldsById.Keys.ToHashSet(StringComparer.Ordinal);

            if (action.PrintTemplateId is not null && !string.Equals(action.Type, TriggerActionTypes.SendEmail, StringComparison.Ordinal))
            {
                errors.Add(Error($"{path}.printTemplateId", "trigger.action.pdf_attachment_type", "PDF print template attachments are only supported for email actions."));
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

                    ValidateEmailPdfAttachment(action, printTemplateTargets, sourceFormId, path, errors);

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
                case TriggerActionTypes.UpdateField:
                    ValidateKnownField(action.FieldId, fieldIds, $"{path}.fieldId", errors);

                    if (IsMissingActionValue(action.Value))
                    {
                        errors.Add(Error($"{path}.value", "trigger.action.value_required", "Update field action value is required."));
                    }

                    break;
                case TriggerActionTypes.SendNotification:
                    ValidateNotificationAction(action, activeUserIds, activeGroupIds, path, errors);
                    break;
                case TriggerActionTypes.CreateRecord:
                    ValidateCreateRecordAction(action, sourceFieldsById, targetForms, path, errors);
                    break;
                case TriggerActionTypes.CallWebhook:
                    ValidateWebhookAction(action, path, errors);
                    break;
                case TriggerActionTypes.StartWorkflow:
                    ValidateStartWorkflowAction(action, workflowStartTargets, sourceFormId, path, errors);
                    break;
            }
        }
    }

    private static void ValidateStartWorkflowAction(
        TriggerActionDefinition action,
        IReadOnlyCollection<TriggerWorkflowStartTarget> workflowStartTargets,
        Guid? sourceFormId,
        string path,
        List<TriggerValidationError> errors)
    {
        if (action.WorkflowDefinitionId is null || action.WorkflowDefinitionId == Guid.Empty)
        {
            errors.Add(Error($"{path}.workflowDefinitionId", "trigger.action.workflow_required", "Start workflow action requires a published workflow."));
            return;
        }

        var workflow = workflowStartTargets.FirstOrDefault(candidate => candidate.WorkflowDefinitionId == action.WorkflowDefinitionId.Value);

        if (workflow is null)
        {
            errors.Add(Error($"{path}.workflowDefinitionId", "trigger.action.workflow_missing", "Workflow target was not found."));
            return;
        }

        if (sourceFormId is not null && workflow.FormId != sourceFormId.Value)
        {
            errors.Add(Error($"{path}.workflowDefinitionId", "trigger.action.workflow_form", "Workflow target must belong to the same form as the trigger."));
        }

        if (!workflow.IsEnabled)
        {
            errors.Add(Error($"{path}.workflowDefinitionId", "trigger.action.workflow_disabled", "Workflow target must be enabled."));
        }

        if (!string.Equals(workflow.Status, WorkflowDefinitionStatuses.Published, StringComparison.Ordinal)
            || workflow.CurrentVersionId is null
            || workflow.CurrentVersionId == Guid.Empty)
        {
            errors.Add(Error($"{path}.workflowDefinitionId", "trigger.action.workflow_published", "Workflow target must be published with a current version."));
        }
    }

    private static void ValidateEmailPdfAttachment(
        TriggerActionDefinition action,
        IReadOnlyCollection<TriggerPrintTemplateTarget> printTemplateTargets,
        Guid? sourceFormId,
        string path,
        List<TriggerValidationError> errors)
    {
        if (action.PrintTemplateId is null)
        {
            return;
        }

        var printTemplateId = action.PrintTemplateId.Value;
        var target = printTemplateTargets.FirstOrDefault(candidate => candidate.PrintTemplateId == printTemplateId);

        if (target is null)
        {
            errors.Add(Error($"{path}.printTemplateId", "trigger.action.print_template_missing", "Email PDF attachment template was not found."));
            return;
        }

        if (!string.Equals(target.Type, PrintTemplateTypes.Record, StringComparison.Ordinal))
        {
            errors.Add(Error($"{path}.printTemplateId", "trigger.action.print_template_type", "Email PDF attachments require a record print template."));
        }

        if (target.CurrentVersionId is null || target.CurrentVersionId == Guid.Empty)
        {
            errors.Add(Error($"{path}.printTemplateId", "trigger.action.print_template_unpublished", "Email PDF attachment template must have a published version."));
        }

        if (sourceFormId is not null && target.FormId != sourceFormId.Value)
        {
            errors.Add(Error($"{path}.printTemplateId", "trigger.action.print_template_form", "Email PDF attachment template must belong to this trigger form."));
        }
    }

    private static void ValidateWebhookAction(
        TriggerActionDefinition action,
        string path,
        List<TriggerValidationError> errors)
    {
        if (!Uri.TryCreate(action.WebhookUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add(Error($"{path}.webhookUrl", "trigger.action.webhook_url", "Webhook action requires an absolute http or https URL."));
        }

        if (!IsSupportedWebhookMethod(action.WebhookMethod))
        {
            errors.Add(Error($"{path}.webhookMethod", "trigger.action.webhook_method", "Webhook action method must be GET, POST, PUT, PATCH, or DELETE."));
        }

        foreach (var header in action.WebhookHeaders ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(header.Key))
            {
                errors.Add(Error($"{path}.webhookHeaders", "trigger.action.webhook_header", "Webhook header names cannot be blank."));
            }
        }
    }

    private static void ValidateCreateRecordAction(
        TriggerActionDefinition action,
        IReadOnlyDictionary<string, FormFieldDefinition> sourceFieldsById,
        IReadOnlyCollection<TriggerTargetFormSchema> targetForms,
        string path,
        List<TriggerValidationError> errors)
    {
        if (action.TargetFormId is null || action.TargetFormId == Guid.Empty)
        {
            errors.Add(Error($"{path}.targetFormId", "trigger.action.target_form_required", "Create record action requires a published target form."));
            return;
        }

        var targetForm = targetForms.FirstOrDefault(form => form.FormId == action.TargetFormId.Value);

        if (targetForm is null)
        {
            errors.Add(Error($"{path}.targetFormId", "trigger.action.target_form_missing", "Create record target form is not published or was not found."));
            return;
        }

        var values = action.Values ?? new Dictionary<string, TriggerActionValueDefinition>();

        if (values.Count == 0)
        {
            errors.Add(Error($"{path}.values", "trigger.action.values_required", "Create record action requires target field values."));
            return;
        }

        var targetFieldsById = targetForm.Schema.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var validationValues = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (fieldId, valueDefinition) in values)
        {
            var valuePath = $"{path}.values.{fieldId}";

            if (!targetFieldsById.TryGetValue(fieldId, out var targetField))
            {
                errors.Add(Error(valuePath, "trigger.action.target_field_unknown", "Create record target field does not exist on the target form."));
                validationValues[fieldId] = ResolveValidationValue(valueDefinition, targetField: null);
                continue;
            }

            var hasSourceField = !string.IsNullOrWhiteSpace(valueDefinition.SourceFieldId);
            var hasLiteral = !IsMissingActionValue(valueDefinition.Literal);

            if (hasSourceField == hasLiteral)
            {
                errors.Add(Error(valuePath, "trigger.action.value_source", "Each create record value must use exactly one literal value or source field."));
                validationValues[fieldId] = ResolveValidationValue(valueDefinition, targetField);
                continue;
            }

            if (hasSourceField)
            {
                if (!sourceFieldsById.TryGetValue(valueDefinition.SourceFieldId!, out var sourceField))
                {
                    errors.Add(Error($"{valuePath}.sourceFieldId", "trigger.action.source_field_unknown", "Create record source field does not exist on the source form."));
                }
                else if (!string.Equals(sourceField.Type, targetField.Type, StringComparison.Ordinal))
                {
                    errors.Add(Error($"{valuePath}.sourceFieldId", "trigger.action.source_field_type", "Create record source field type must match the target field type."));
                }
            }

            validationValues[fieldId] = ResolveValidationValue(valueDefinition, targetField);
        }

        var targetValidation = FormSchemaValidator.ValidateRecordValues(targetForm.Schema, validationValues);
        errors.AddRange(targetValidation.Errors.Select(error => Error($"{path}.{error.Path}", error.Code, error.Message)));
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

    private static void ValidateNotificationAction(
        TriggerActionDefinition action,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        string path,
        List<TriggerValidationError> errors)
    {
        var userIds = action.RecipientUserIds ?? Array.Empty<Guid>();
        var groupIds = action.RecipientGroupIds ?? Array.Empty<Guid>();

        if (string.IsNullOrWhiteSpace(action.Title))
        {
            errors.Add(Error($"{path}.title", "trigger.action.notification_title_required", "Notification title is required."));
        }

        if (string.IsNullOrWhiteSpace(action.Body))
        {
            errors.Add(Error($"{path}.body", "trigger.action.notification_body_required", "Notification body is required."));
        }

        if (userIds.Count == 0 && groupIds.Count == 0)
        {
            errors.Add(Error($"{path}.recipients", "trigger.action.notification_recipients_required", "Notification action requires at least one user or group recipient."));
        }

        foreach (var userId in userIds)
        {
            if (!activeUserIds.Contains(userId))
            {
                errors.Add(Error($"{path}.recipientUserIds", "trigger.action.notification_user_missing", "Notification recipient user is not active or was not found."));
            }
        }

        foreach (var groupId in groupIds)
        {
            if (!activeGroupIds.Contains(groupId))
            {
                errors.Add(Error($"{path}.recipientGroupIds", "trigger.action.notification_group_missing", "Notification recipient group is not active or was not found."));
            }
        }
    }

    private static IReadOnlyList<Guid>? NormalizeIds(IReadOnlyList<Guid>? ids)
    {
        return ids?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    private static Guid? NormalizeNullableId(Guid? id)
    {
        return id is null || id == Guid.Empty ? null : id;
    }

    private static IReadOnlyDictionary<string, TriggerActionValueDefinition>? NormalizeActionValues(
        IReadOnlyDictionary<string, TriggerActionValueDefinition>? values)
    {
        if (values is null)
        {
            return null;
        }

        return values
            .Select(pair => new
            {
                FieldId = Normalize(pair.Key),
                Value = pair.Value with
                {
                    Literal = NormalizeActionValue(pair.Value.Literal),
                    SourceFieldId = NormalizeOptional(pair.Value.SourceFieldId)
                }
            })
            .Where(pair => pair.FieldId.Length > 0)
            .GroupBy(pair => pair.FieldId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, string>? NormalizeWebhookHeaders(
        IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null)
        {
            return null;
        }

        return headers
            .Select(header => new { Key = header.Key.Trim(), Value = header.Value.Trim() })
            .Where(header => header.Key.Length > 0)
            .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);
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

    private static object? NormalizeActionValue(object? value)
    {
        return value is string stringValue ? stringValue.Trim() : value;
    }

    private static string NormalizeWebhookMethod(string? value)
    {
        var normalized = Normalize(value).ToUpperInvariant();
        return normalized.Length == 0 ? "POST" : normalized;
    }

    private static bool IsSupportedWebhookMethod(string? value)
    {
        return NormalizeWebhookMethod(value) is "GET" or "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private static object? ResolveValidationValue(TriggerActionValueDefinition valueDefinition, FormFieldDefinition? targetField)
    {
        if (!string.IsNullOrWhiteSpace(valueDefinition.SourceFieldId) && targetField is not null)
        {
            return PlaceholderValue(targetField);
        }

        return valueDefinition.Literal;
    }

    private static object? PlaceholderValue(FormFieldDefinition field)
    {
        return field.Type switch
        {
            FormFieldTypes.Number => 1,
            FormFieldTypes.Checkbox => true,
            FormFieldTypes.Email => "source@example.test",
            FormFieldTypes.Date => "2026-01-01",
            FormFieldTypes.Select or FormFieldTypes.Radio => field.Options?.FirstOrDefault()?.Value ?? string.Empty,
            _ => "source value"
        };
    }

    private static bool IsMissingActionValue(object? value)
    {
        return value is null
            || value is string { Length: 0 }
            || value is System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined }
            || value is System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } element && string.IsNullOrEmpty(element.GetString());
    }
}
