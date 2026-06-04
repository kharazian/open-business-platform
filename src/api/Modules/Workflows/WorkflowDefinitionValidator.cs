namespace OpenBusinessPlatform.Api.Modules.Workflows;

public static class WorkflowDefinitionValidator
{
    private const int MaxKeyLength = 80;
    private const int MaxNameLength = 200;

    public static WorkflowValidationResult Validate(
        CreateWorkflowDefinitionRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<Guid> activeDepartmentIds)
    {
        return ValidateDefinition(request.Name, request.Config, activeUserIds, activeGroupIds, activeDepartmentIds);
    }

    public static WorkflowValidationResult Validate(
        UpdateWorkflowDefinitionRequest request,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<Guid> activeDepartmentIds)
    {
        var result = ValidateDefinition(request.Name, request.Config, activeUserIds, activeGroupIds, activeDepartmentIds);
        var errors = result.Errors.ToList();

        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp))
        {
            errors.Add(Error("concurrencyStamp", "workflow.concurrency_required", "Concurrency stamp is required."));
        }

        return new WorkflowValidationResult(errors);
    }

    public static WorkflowDefinitionConfig NormalizeConfig(WorkflowDefinitionConfig? config)
    {
        if (config is null)
        {
            return new WorkflowDefinitionConfig(1, string.Empty, Array.Empty<WorkflowStateDefinition>(), Array.Empty<WorkflowTransitionDefinition>(), Array.Empty<WorkflowApprovalStepDefinition>());
        }

        return new WorkflowDefinitionConfig(
            config.SchemaVersion <= 0 ? 1 : config.SchemaVersion,
            NormalizeKey(config.InitialStateKey),
            (config.States ?? Array.Empty<WorkflowStateDefinition>())
                .Select(state => new WorkflowStateDefinition(NormalizeKey(state.Key), NormalizeName(state.Name), state.IsFinal))
                .ToArray(),
            (config.Transitions ?? Array.Empty<WorkflowTransitionDefinition>())
                .Select(transition => new WorkflowTransitionDefinition(
                    NormalizeKey(transition.Key),
                    NormalizeName(transition.Name),
                    NormalizeKey(transition.FromStateKey),
                    NormalizeKey(transition.ToStateKey),
                    NormalizeOptionalKey(transition.ApprovalStepKey),
                    NormalizeActions(transition.Actions)))
                .ToArray(),
            (config.ApprovalSteps ?? Array.Empty<WorkflowApprovalStepDefinition>())
                .Select(step => new WorkflowApprovalStepDefinition(
                    NormalizeKey(step.Key),
                    NormalizeName(step.Name),
                    NormalizeKey(step.Mode),
                    (step.AssigneeRules ?? Array.Empty<WorkflowAssigneeRuleDefinition>())
                        .Select(rule => new WorkflowAssigneeRuleDefinition(NormalizeKey(rule.Type), rule.UserId, rule.GroupId, rule.DepartmentId))
                        .ToArray()))
                .ToArray());
    }

    private static WorkflowValidationResult ValidateDefinition(
        string name,
        WorkflowDefinitionConfig? config,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<Guid> activeDepartmentIds)
    {
        var errors = new List<WorkflowValidationError>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(Error("name", "workflow.name_required", "Workflow name is required."));
        }
        else if (name.Trim().Length > MaxNameLength)
        {
            errors.Add(Error("name", "workflow.name_too_long", "Workflow name must be 200 characters or fewer."));
        }

        var normalized = NormalizeConfig(config);
        ValidateStates(normalized, errors);
        ValidateApprovalSteps(normalized, activeUserIds, activeGroupIds, activeDepartmentIds, errors);
        ValidateTransitions(normalized, errors);

        return new WorkflowValidationResult(errors);
    }

    private static void ValidateStates(WorkflowDefinitionConfig config, ICollection<WorkflowValidationError> errors)
    {
        if (config.States.Count == 0)
        {
            errors.Add(Error("config.states", "workflow.states_required", "At least one workflow state is required."));
            return;
        }

        var stateKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < config.States.Count; index += 1)
        {
            var state = config.States[index];
            var path = $"config.states[{index}]";

            ValidateKey(state.Key, $"{path}.key", "workflow.state_key_required", "Workflow state keys are required.", errors);

            if (string.IsNullOrWhiteSpace(state.Name))
            {
                errors.Add(Error($"{path}.name", "workflow.state_name_required", "Workflow state names are required."));
            }

            if (!string.IsNullOrWhiteSpace(state.Key) && !stateKeys.Add(state.Key))
            {
                errors.Add(Error($"{path}.key", "workflow.state_key_duplicate", "Workflow state keys must be unique."));
            }
        }

        if (string.IsNullOrWhiteSpace(config.InitialStateKey))
        {
            errors.Add(Error("config.initialStateKey", "workflow.initial_state_required", "Initial state key is required."));
        }
        else if (!stateKeys.Contains(config.InitialStateKey))
        {
            errors.Add(Error("config.initialStateKey", "workflow.initial_state_missing", "Initial state must reference an existing state."));
        }

        var initialState = config.States.FirstOrDefault(state => string.Equals(state.Key, config.InitialStateKey, StringComparison.Ordinal));
        if (initialState?.IsFinal == true)
        {
            errors.Add(Error("config.initialStateKey", "workflow.initial_state_final", "Initial state cannot be a final state."));
        }

        if (!config.States.Any(state => state.IsFinal))
        {
            errors.Add(Error("config.states", "workflow.final_state_required", "At least one final state is required."));
        }
    }

    private static void ValidateTransitions(WorkflowDefinitionConfig config, ICollection<WorkflowValidationError> errors)
    {
        if (config.Transitions.Count == 0)
        {
            errors.Add(Error("config.transitions", "workflow.transitions_required", "At least one workflow transition is required."));
            return;
        }

        var stateKeys = config.States.Select(state => state.Key).ToHashSet(StringComparer.Ordinal);
        var finalStateKeys = config.States.Where(state => state.IsFinal).Select(state => state.Key).ToHashSet(StringComparer.Ordinal);
        var approvalStepKeys = config.ApprovalSteps.Select(step => step.Key).ToHashSet(StringComparer.Ordinal);
        var transitionKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < config.Transitions.Count; index += 1)
        {
            var transition = config.Transitions[index];
            var path = $"config.transitions[{index}]";

            ValidateKey(transition.Key, $"{path}.key", "workflow.transition_key_required", "Workflow transition keys are required.", errors);

            if (string.IsNullOrWhiteSpace(transition.Name))
            {
                errors.Add(Error($"{path}.name", "workflow.transition_name_required", "Workflow transition names are required."));
            }

            if (!string.IsNullOrWhiteSpace(transition.Key) && !transitionKeys.Add(transition.Key))
            {
                errors.Add(Error($"{path}.key", "workflow.transition_key_duplicate", "Workflow transition keys must be unique."));
            }

            if (string.IsNullOrWhiteSpace(transition.FromStateKey) || !stateKeys.Contains(transition.FromStateKey))
            {
                errors.Add(Error($"{path}.fromStateKey", "workflow.transition_from_missing", "Transition from-state must reference an existing state."));
            }
            else if (finalStateKeys.Contains(transition.FromStateKey))
            {
                errors.Add(Error($"{path}.fromStateKey", "workflow.transition_from_final", "Transitions cannot start from a final state."));
            }

            if (string.IsNullOrWhiteSpace(transition.ToStateKey) || !stateKeys.Contains(transition.ToStateKey))
            {
                errors.Add(Error($"{path}.toStateKey", "workflow.transition_to_missing", "Transition to-state must reference an existing state."));
            }

            if (!string.IsNullOrWhiteSpace(transition.FromStateKey)
                && string.Equals(transition.FromStateKey, transition.ToStateKey, StringComparison.Ordinal))
            {
                errors.Add(Error($"{path}.toStateKey", "workflow.transition_same_state", "Transition endpoints must be different states."));
            }

            if (!string.IsNullOrWhiteSpace(transition.ApprovalStepKey) && !approvalStepKeys.Contains(transition.ApprovalStepKey))
            {
                errors.Add(Error($"{path}.approvalStepKey", "workflow.transition_approval_missing", "Transition approval step must reference an existing approval step."));
            }

            ValidateActions(transition.Actions ?? Array.Empty<WorkflowActionDefinition>(), path, errors);
        }
    }

    private static void ValidateApprovalSteps(
        WorkflowDefinitionConfig config,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> activeGroupIds,
        IReadOnlyCollection<Guid> activeDepartmentIds,
        ICollection<WorkflowValidationError> errors)
    {
        var stepKeys = new HashSet<string>(StringComparer.Ordinal);
        var activeUsers = activeUserIds.ToHashSet();
        var activeGroups = activeGroupIds.ToHashSet();
        var activeDepartments = activeDepartmentIds.ToHashSet();

        for (var index = 0; index < config.ApprovalSteps.Count; index += 1)
        {
            var step = config.ApprovalSteps[index];
            var path = $"config.approvalSteps[{index}]";

            ValidateKey(step.Key, $"{path}.key", "workflow.approval_key_required", "Workflow approval step keys are required.", errors);

            if (string.IsNullOrWhiteSpace(step.Name))
            {
                errors.Add(Error($"{path}.name", "workflow.approval_name_required", "Workflow approval step names are required."));
            }

            if (!string.IsNullOrWhiteSpace(step.Key) && !stepKeys.Add(step.Key))
            {
                errors.Add(Error($"{path}.key", "workflow.approval_key_duplicate", "Workflow approval step keys must be unique."));
            }

            if (!WorkflowApprovalModes.Supported.Contains(step.Mode))
            {
                errors.Add(Error($"{path}.mode", "workflow.approval_mode_invalid", "Workflow approval mode is invalid."));
            }

            if (step.AssigneeRules.Count == 0)
            {
                errors.Add(Error($"{path}.assigneeRules", "workflow.approval_assignees_required", "Approval steps require at least one assignee rule."));
                continue;
            }

            for (var ruleIndex = 0; ruleIndex < step.AssigneeRules.Count; ruleIndex += 1)
            {
                ValidateAssigneeRule(step.AssigneeRules[ruleIndex], $"{path}.assigneeRules[{ruleIndex}]", activeUsers, activeGroups, activeDepartments, errors);
            }
        }
    }

    private static void ValidateAssigneeRule(
        WorkflowAssigneeRuleDefinition rule,
        string path,
        IReadOnlySet<Guid> activeUserIds,
        IReadOnlySet<Guid> activeGroupIds,
        IReadOnlySet<Guid> activeDepartmentIds,
        ICollection<WorkflowValidationError> errors)
    {
        if (!WorkflowAssigneeRuleTypes.Supported.Contains(rule.Type))
        {
            errors.Add(Error($"{path}.type", "workflow.assignee_type_invalid", "Workflow assignee rule type is invalid."));
            return;
        }

        switch (rule.Type)
        {
            case WorkflowAssigneeRuleTypes.User:
                if (rule.UserId is null || rule.UserId == Guid.Empty || !activeUserIds.Contains(rule.UserId.Value))
                {
                    errors.Add(Error($"{path}.userId", "workflow.assignee_user_missing", "User assignee must reference an active user."));
                }

                break;
            case WorkflowAssigneeRuleTypes.Group:
                if (rule.GroupId is null || rule.GroupId == Guid.Empty || !activeGroupIds.Contains(rule.GroupId.Value))
                {
                    errors.Add(Error($"{path}.groupId", "workflow.assignee_group_missing", "Group assignee must reference an active group."));
                }

                break;
            case WorkflowAssigneeRuleTypes.DepartmentManager:
                if (rule.DepartmentId is null || rule.DepartmentId == Guid.Empty || !activeDepartmentIds.Contains(rule.DepartmentId.Value))
                {
                    errors.Add(Error($"{path}.departmentId", "workflow.assignee_department_missing", "Department manager assignee must reference an active department."));
                }

                break;
            case WorkflowAssigneeRuleTypes.RecordOwner:
                break;
        }
    }

    private static void ValidateActions(
        IReadOnlyList<WorkflowActionDefinition> actions,
        string transitionPath,
        ICollection<WorkflowValidationError> errors)
    {
        var actionIds = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < actions.Count; index += 1)
        {
            var action = actions[index];
            var path = $"{transitionPath}.actions[{index}]";

            ValidateKey(action.Id, $"{path}.id", "workflow.action_id_required", "Workflow action IDs are required.", errors);

            if (!string.IsNullOrWhiteSpace(action.Id) && !actionIds.Add(action.Id))
            {
                errors.Add(Error($"{path}.id", "workflow.action_id_duplicate", "Workflow action IDs must be unique within a transition."));
            }

            if (!WorkflowActionTypes.Supported.Contains(action.Type))
            {
                errors.Add(Error($"{path}.type", "workflow.action_type_invalid", "Workflow action type is invalid."));
            }
        }
    }

    private static IReadOnlyList<WorkflowActionDefinition> NormalizeActions(IReadOnlyList<WorkflowActionDefinition>? actions)
    {
        return (actions ?? Array.Empty<WorkflowActionDefinition>())
            .Select(action => new WorkflowActionDefinition(
                NormalizeKey(action.Id),
                NormalizeKey(action.Type),
                NormalizeOptionalText(action.Message),
                action.To,
                NormalizeOptionalText(action.Subject),
                NormalizeOptionalText(action.Body),
                NormalizeOptionalKey(action.Status),
                action.AssignedToUserId,
                action.AssignedGroupId,
                NormalizeOptionalKey(action.FieldId),
                action.Value,
                NormalizeOptionalText(action.Title),
                action.RecipientUserIds,
                action.RecipientGroupIds,
                action.TargetFormId,
                action.Values))
            .ToArray();
    }

    private static void ValidateKey(string value, string path, string code, string message, ICollection<WorkflowValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(Error(path, code, message));
            return;
        }

        if (value.Length > MaxKeyLength)
        {
            errors.Add(Error(path, "workflow.key_too_long", "Workflow keys must be 80 characters or fewer."));
        }
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalKey(string? value)
    {
        var normalized = NormalizeKey(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static string NormalizeName(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static WorkflowValidationError Error(string path, string code, string message)
    {
        return new WorkflowValidationError(path, code, message);
    }
}
