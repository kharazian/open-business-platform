import type { EntityId } from "../../types/entities";
import type { FormField, FormRecordValue, FormSchema } from "../forms/types";
import {
  isTriggerActionType,
  isTriggerConditionType,
  isTriggerEventName,
  type CreateTriggerRequest,
  type TriggerActionDefinition,
  type TriggerActionType,
  type TriggerActionValueDefinition,
  type TriggerConditionDefinition,
  type TriggerConditionType,
  type TriggerDetail,
  type TriggerEventName,
  type TriggerExecutionStatus,
  type TriggerRetryPolicyDefinition,
  type TriggerScheduleDefinition,
  type TriggerValidationError
} from "./types";

export type TriggerLogStatusTone = "default" | "success" | "danger";

export type TriggerFieldOption = {
  id: string;
  label: string;
  type: FormField["type"];
  options: Array<{ label: string; value: string }>;
};

export type TriggerConditionDraft = {
  clientId: string;
  type: TriggerConditionType;
  fieldId?: string;
  value?: FormRecordValue;
  status?: string;
  departmentId?: EntityId;
  userId?: EntityId;
  groupId?: EntityId;
};

export type TriggerActionDraft = {
  clientId: string;
  id: string;
  type: TriggerActionType;
  message?: string;
  toText?: string;
  subject?: string;
  body?: string;
  status?: string;
  assignedToUserId?: EntityId;
  assignedGroupId?: EntityId;
  fieldId?: string;
  value?: FormRecordValue;
  title?: string;
  recipientUserId?: EntityId;
  recipientGroupId?: EntityId;
  targetFormId?: EntityId;
  valueMappingsText?: string;
  webhookUrl?: string;
  webhookMethod?: string;
  webhookHeadersText?: string;
  workflowDefinitionId?: EntityId;
  printTemplateId?: EntityId;
};

export type TriggerDraft = {
  id?: EntityId;
  formId?: EntityId;
  name: string;
  description: string;
  eventName: TriggerEventName;
  conditions: TriggerConditionDraft[];
  actions: TriggerActionDraft[];
  isEnabled: boolean;
  retryPolicy: TriggerRetryPolicyDefinition;
  schedule: TriggerScheduleDefinition | null;
  concurrencyStamp?: string;
};

export type TriggerDraftValidationResult = {
  valid: boolean;
  errors: TriggerValidationError[];
};

export const triggerEventOptions: Array<{ label: string; value: TriggerEventName }> = [
  { label: "Record created", value: "record.created" },
  { label: "Record updated", value: "record.updated" },
  { label: "Field changed", value: "field.changed" },
  { label: "Status changed", value: "status.changed" },
  { label: "Record assigned", value: "record.assigned" },
  { label: "Schedule once", value: "schedule.once" },
  { label: "Schedule daily", value: "schedule.daily" },
  { label: "Schedule weekly", value: "schedule.weekly" },
  { label: "Schedule monthly", value: "schedule.monthly" }
];

export const triggerConditionOptions: Array<{ label: string; value: TriggerConditionType }> = [
  { label: "Field equals", value: "field_equals" },
  { label: "Field changed", value: "field_changed" },
  { label: "Status changed to", value: "status_changed_to" },
  { label: "Department equals", value: "department_equals" },
  { label: "Assigned to user", value: "assigned_to_user" },
  { label: "Assigned to group", value: "assigned_to_group" }
];

export const triggerActionOptions: Array<{ label: string; value: TriggerActionType }> = [
  { label: "Write audit entry", value: "write_audit_entry" },
  { label: "Send email", value: "send_email" },
  { label: "Change status", value: "change_status" },
  { label: "Assign record", value: "assign_record" },
  { label: "Update field", value: "update_field" },
  { label: "Send notification", value: "send_notification" },
  { label: "Create record", value: "create_record" },
  { label: "Call webhook", value: "call_webhook" },
  { label: "Start workflow", value: "start_workflow" }
];

export const defaultTriggerRetryPolicy: TriggerRetryPolicyDefinition = {
  isEnabled: true,
  maxAttempts: 3,
  delaySeconds: 60
};

export function createDefaultTriggerSchedule(eventName: TriggerEventName): TriggerScheduleDefinition | null {
  const kind = scheduleKindFromEvent(eventName);

  if (!kind) {
    return null;
  }

  const startAt = new Date();
  const schedule: TriggerScheduleDefinition = {
    kind,
    timeZone: "Etc/UTC",
    startAt: startAt.toISOString(),
    interval: 1
  };

  if (kind === "weekly") {
    schedule.dayOfWeek = startAt.getUTCDay();
  }

  if (kind === "monthly") {
    schedule.dayOfMonth = startAt.getUTCDate();
  }

  return schedule;
}

function normalizeTriggerSchedule(schedule: TriggerScheduleDefinition): TriggerScheduleDefinition {
  const normalized: TriggerScheduleDefinition = {
    kind: schedule.kind,
    timeZone: schedule.timeZone,
    startAt: schedule.startAt,
    interval: schedule.interval ?? 1
  };

  if (schedule.kind === "weekly") {
    normalized.dayOfWeek = schedule.dayOfWeek ?? new Date(schedule.startAt).getUTCDay();
  }

  if (schedule.kind === "monthly") {
    normalized.dayOfMonth = schedule.dayOfMonth ?? new Date(schedule.startAt).getUTCDate();
  }

  return normalized;
}

function validateScheduleDefinition(schedule: TriggerScheduleDefinition, errors: TriggerValidationError[]) {
  const interval = schedule.interval ?? 1;

  if (interval < 1 || interval > 366) {
    errors.push(error("schedule.interval", "trigger.schedule.interval", "Enter a schedule interval between 1 and 366."));
  }

  if (schedule.kind === "weekly" && schedule.dayOfWeek != null && (schedule.dayOfWeek < 0 || schedule.dayOfWeek > 6)) {
    errors.push(error("schedule.dayOfWeek", "trigger.schedule.day_of_week", "Enter a weekly day from 0 to 6."));
  }

  if (schedule.kind === "monthly" && schedule.dayOfMonth != null && (schedule.dayOfMonth < 1 || schedule.dayOfMonth > 31)) {
    errors.push(error("schedule.dayOfMonth", "trigger.schedule.day_of_month", "Enter a monthly day from 1 to 31."));
  }
}

export function createEmptyTriggerDraft(formName = "Form"): TriggerDraft {
  return {
    name: `${formName.trim() || "Form"} automation`,
    description: "",
    eventName: "record.created",
    conditions: [],
    actions: [createTriggerActionDraft("write_audit_entry", 1)],
    isEnabled: true,
    retryPolicy: defaultTriggerRetryPolicy,
    schedule: null
  };
}

export function createTriggerConditionDraft(type: TriggerConditionType = "field_equals", index = Date.now()): TriggerConditionDraft {
  return {
    clientId: `condition-${index}`,
    type,
    fieldId: "",
    value: "",
    status: "",
    departmentId: "",
    userId: "",
    groupId: ""
  };
}

export function createTriggerActionDraft(type: TriggerActionType = "write_audit_entry", index = Date.now()): TriggerActionDraft {
  return {
    clientId: `action-${index}`,
    id: `action-${index}`,
    type,
    message: type === "write_audit_entry" ? "Trigger matched." : "",
    toText: "",
    subject: "",
    body: "",
    status: "",
    assignedToUserId: "",
    assignedGroupId: "",
    fieldId: "",
    value: "",
    title: type === "send_notification" ? "Record needs review" : "",
    recipientUserId: "",
    recipientGroupId: "",
    targetFormId: "",
    valueMappingsText: type === "create_record" ? "{\n  \"field_id\": { \"literal\": \"value\" }\n}" : "",
    webhookUrl: "",
    webhookMethod: "POST",
    webhookHeadersText: type === "call_webhook" ? "{}" : "",
    workflowDefinitionId: "",
    printTemplateId: ""
  };
}

export function createTriggerDraftFromDetail(detail: TriggerDetail): TriggerDraft {
  return {
    id: detail.id,
    formId: detail.formId,
    name: detail.name,
    description: detail.description ?? "",
    eventName: detail.eventName,
    conditions: detail.conditions.conditions.map((condition, index) => createConditionDraftFromDefinition(condition, index + 1)),
    actions: detail.actions.map((action, index) => createActionDraftFromDefinition(action, index + 1)),
    isEnabled: detail.isEnabled,
    retryPolicy: detail.retryPolicy ?? defaultTriggerRetryPolicy,
    schedule: detail.schedule ?? null,
    concurrencyStamp: detail.concurrencyStamp
  };
}

export function getTriggerFieldOptions(schema?: FormSchema | null): TriggerFieldOption[] {
  return (schema?.fields ?? []).map((field) => ({
    id: field.id,
    label: field.label,
    type: field.type,
    options: (field.options ?? []).map((option) => ({ label: option.label, value: option.value }))
  }));
}

export function buildTriggerRequest(draft: TriggerDraft): CreateTriggerRequest {
  return {
    name: draft.name.trim(),
    description: normalizeOptionalText(draft.description),
    eventName: draft.eventName,
    conditions: {
      mode: "all",
      conditions: draft.conditions.map(buildCondition)
    },
    actions: draft.actions.map(buildAction),
    isEnabled: draft.isEnabled,
    retryPolicy: normalizeRetryPolicy(draft.retryPolicy),
    schedule: isScheduledTriggerEvent(draft.eventName) && draft.schedule ? normalizeTriggerSchedule(draft.schedule) : null
  };
}

export function validateTriggerDraft(draft: TriggerDraft): TriggerDraftValidationResult {
  const errors: TriggerValidationError[] = [];

  if (!draft.name.trim()) {
    errors.push(error("name", "trigger.name.required", "Trigger name is required."));
  }

  if (!isTriggerEventName(draft.eventName)) {
    errors.push(error("eventName", "trigger.event.unsupported", "Choose a supported trigger event."));
  }

  if (draft.retryPolicy.isEnabled) {
    if (draft.retryPolicy.maxAttempts < 1 || draft.retryPolicy.maxAttempts > 10) {
      errors.push(error("retryPolicy.maxAttempts", "trigger.retry.max_attempts", "Enter 1 to 10 retry attempts."));
    }

    if (draft.retryPolicy.delaySeconds < 30 || draft.retryPolicy.delaySeconds > 86400) {
      errors.push(error("retryPolicy.delaySeconds", "trigger.retry.delay", "Enter a retry delay between 30 seconds and 24 hours."));
    }
  }

  if (isScheduledTriggerEvent(draft.eventName)) {
    if (!draft.schedule) {
      errors.push(error("schedule", "trigger.schedule.required", "Enter schedule metadata."));
    } else if (draft.schedule.kind !== scheduleKindFromEvent(draft.eventName)) {
      errors.push(error("schedule.kind", "trigger.schedule.event_kind", "Schedule kind must match the selected event."));
    } else {
      validateScheduleDefinition(draft.schedule, errors);
    }

    if (draft.conditions.length > 0) {
      errors.push(error("conditions", "trigger.schedule.conditions", "Scheduled triggers do not support record conditions."));
    }
  }

  draft.conditions.forEach((condition, index) => {
    const path = `conditions[${index}]`;

    if (!isTriggerConditionType(condition.type)) {
      errors.push(error(`${path}.type`, "trigger.condition.type", "Choose a supported condition type."));
      return;
    }

    if (condition.type === "field_equals" || condition.type === "field_changed") {
      if (!condition.fieldId?.trim()) {
        errors.push(error(`${path}.fieldId`, "trigger.condition.field_required", "Choose a form field."));
      }
    }

    if (condition.type === "field_equals" && isBlank(condition.value)) {
      errors.push(error(`${path}.value`, "trigger.condition.value_required", "Enter the field value to match."));
    }

    if (condition.type === "status_changed_to" && !condition.status?.trim()) {
      errors.push(error(`${path}.status`, "trigger.condition.status_required", "Enter the status to match."));
    }

    if (condition.type === "department_equals" && !condition.departmentId) {
      errors.push(error(`${path}.departmentId`, "trigger.condition.department_required", "Choose a department."));
    }

    if (condition.type === "assigned_to_user" && !condition.userId) {
      errors.push(error(`${path}.userId`, "trigger.condition.user_required", "Choose a user."));
    }

    if (condition.type === "assigned_to_group" && !condition.groupId) {
      errors.push(error(`${path}.groupId`, "trigger.condition.group_required", "Choose a group."));
    }
  });

  if (draft.actions.length === 0) {
    errors.push(error("actions", "trigger.actions.required", "Add at least one action."));
  }

  draft.actions.forEach((action, index) => {
    const path = `actions[${index}]`;

    if (!action.id.trim()) {
      errors.push(error(`${path}.id`, "trigger.action.id_required", "Action id is required."));
    }

    if (!isTriggerActionType(action.type)) {
      errors.push(error(`${path}.type`, "trigger.action.type", "Choose a supported action type."));
      return;
    }

    if (action.type === "write_audit_entry" && !action.message?.trim()) {
      errors.push(error(`${path}.message`, "trigger.action.message_required", "Enter the audit message."));
    }

    if (action.type === "send_email") {
      if (parseRecipients(action.toText).length === 0) {
        errors.push(error(`${path}.to`, "trigger.action.email_to_required", "Enter at least one recipient."));
      }

      if (!action.subject?.trim()) {
        errors.push(error(`${path}.subject`, "trigger.action.email_subject_required", "Enter an email subject."));
      }
    }

    if (action.type === "change_status" && !action.status?.trim()) {
      errors.push(error(`${path}.status`, "trigger.action.status_required", "Enter the status to set."));
    }

    if (action.type === "assign_record") {
      const hasUser = Boolean(action.assignedToUserId);
      const hasGroup = Boolean(action.assignedGroupId);

      if (hasUser === hasGroup) {
        errors.push(error(`${path}.assignment`, "trigger.action.assignment_target", "Choose exactly one user or group."));
      }
    }

    if (action.type === "update_field") {
      if (!action.fieldId?.trim()) {
        errors.push(error(`${path}.fieldId`, "trigger.action.field_required", "Choose a form field to update."));
      }

      if (isBlank(action.value)) {
        errors.push(error(`${path}.value`, "trigger.action.value_required", "Enter the new field value."));
      }
    }

    if (action.type === "send_notification") {
      if (!action.title?.trim()) {
        errors.push(error(`${path}.title`, "trigger.action.notification_title_required", "Enter the notification title."));
      }

      if (!action.body?.trim()) {
        errors.push(error(`${path}.body`, "trigger.action.notification_body_required", "Enter the notification body."));
      }

      if (!action.recipientUserId && !action.recipientGroupId) {
        errors.push(error(`${path}.recipients`, "trigger.action.notification_recipients_required", "Choose at least one user or group."));
      }
    }

    if (action.type === "create_record") {
      if (!action.targetFormId) {
        errors.push(error(`${path}.targetFormId`, "trigger.action.target_form_required", "Choose the target form."));
      }

      if (!parseValueMappings(action.valueMappingsText)) {
        errors.push(error(`${path}.values`, "trigger.action.values_required", "Enter a valid target field value map."));
      }
    }

    if (action.type === "call_webhook") {
      if (!isHttpUrl(action.webhookUrl)) {
        errors.push(error(`${path}.webhookUrl`, "trigger.action.webhook_url", "Enter an absolute http or https URL."));
      }

      if (!isSupportedWebhookMethod(action.webhookMethod)) {
        errors.push(error(`${path}.webhookMethod`, "trigger.action.webhook_method", "Choose GET, POST, PUT, PATCH, or DELETE."));
      }

      if (!parseWebhookHeaders(action.webhookHeadersText)) {
        errors.push(error(`${path}.webhookHeaders`, "trigger.action.webhook_headers", "Enter valid webhook headers JSON."));
      }
    }

    if (action.type === "start_workflow" && !action.workflowDefinitionId) {
      errors.push(error(`${path}.workflowDefinitionId`, "trigger.action.workflow_required", "Choose the workflow to start."));
    }

    if (isScheduledTriggerEvent(draft.eventName) && action.type !== "send_email" && action.type !== "call_webhook") {
      errors.push(error(`${path}.type`, "trigger.schedule.action_type", "Scheduled triggers support email and webhook actions."));
    }

    if (isScheduledTriggerEvent(draft.eventName) && action.printTemplateId) {
      errors.push(error(`${path}.printTemplateId`, "trigger.schedule.pdf_attachment", "Scheduled email PDF attachments require a record trigger."));
    }
  });

  return { valid: errors.length === 0, errors };
}

export function formatTriggerEventLabel(eventName: string): string {
  return triggerEventOptions.find((option) => option.value === eventName)?.label ?? sentenceCase(eventName);
}

export function formatTriggerConditionLabel(type: string): string {
  return triggerConditionOptions.find((option) => option.value === type)?.label ?? sentenceCase(type);
}

export function formatTriggerActionLabel(type: string): string {
  return triggerActionOptions.find((option) => option.value === type)?.label ?? sentenceCase(type);
}

export function formatTriggerLogStatus(status: string): { label: string; variant: TriggerLogStatusTone } {
  if (status === "success") {
    return { label: "Success", variant: "success" };
  }

  if (status === "failed") {
    return { label: "Failed", variant: "danger" };
  }

  return { label: sentenceCase(status), variant: "default" };
}

export function formatTriggerRetryState(state?: string | null, attemptCount = 0, maxAttempts = 0): string {
  if (!state) {
    return "";
  }

  const attempts = maxAttempts > 0 ? ` (${attemptCount}/${maxAttempts})` : "";

  if (state === "pending") {
    return `Retry pending${attempts}`;
  }

  if (state === "completed") {
    return `Retry completed${attempts}`;
  }

  if (state === "exhausted") {
    return `Retries exhausted${attempts}`;
  }

  if (state === "disabled") {
    return "Retries disabled";
  }

  return sentenceCase(state);
}

export function formatTriggerJson(value: unknown): string {
  if (value === null || value === undefined) {
    return "No details";
  }

  return JSON.stringify(value, null, 2);
}

export function formatTriggerDate(value?: string | null): string {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function createConditionDraftFromDefinition(condition: TriggerConditionDefinition, index: number): TriggerConditionDraft {
  return {
    clientId: `condition-${index}`,
    type: condition.type,
    fieldId: condition.fieldId ?? "",
    value: condition.value ?? "",
    status: condition.status ?? "",
    departmentId: condition.departmentId ?? "",
    userId: condition.userId ?? "",
    groupId: condition.groupId ?? ""
  };
}

function createActionDraftFromDefinition(action: TriggerActionDefinition, index: number): TriggerActionDraft {
  return {
    clientId: action.id || `action-${index}`,
    id: action.id || `action-${index}`,
    type: action.type,
    message: action.message ?? "",
    toText: (action.to ?? []).join(", "),
    subject: action.subject ?? "",
    body: action.body ?? "",
    status: action.status ?? "",
    assignedToUserId: action.assignedToUserId ?? "",
    assignedGroupId: action.assignedGroupId ?? "",
    fieldId: action.fieldId ?? "",
    value: action.value ?? "",
    title: action.title ?? "",
    recipientUserId: action.recipientUserIds?.[0] ?? "",
    recipientGroupId: action.recipientGroupIds?.[0] ?? "",
    targetFormId: action.targetFormId ?? "",
    valueMappingsText: formatValueMappingsText(action.values),
    webhookUrl: action.webhookUrl ?? "",
    webhookMethod: action.webhookMethod ?? "POST",
    webhookHeadersText: formatWebhookHeadersText(action.webhookHeaders),
    workflowDefinitionId: action.workflowDefinitionId ?? "",
    printTemplateId: action.printTemplateId ?? ""
  };
}

function buildCondition(condition: TriggerConditionDraft): TriggerConditionDefinition {
  if (condition.type === "field_equals") {
    return { type: condition.type, fieldId: condition.fieldId?.trim() || null, value: normalizeRecordValue(condition.value) };
  }

  if (condition.type === "field_changed") {
    return { type: condition.type, fieldId: condition.fieldId?.trim() || null };
  }

  if (condition.type === "status_changed_to") {
    return { type: condition.type, status: normalizeOptionalText(condition.status) };
  }

  if (condition.type === "department_equals") {
    return { type: condition.type, departmentId: condition.departmentId || null };
  }

  if (condition.type === "assigned_to_user") {
    return { type: condition.type, userId: condition.userId || null };
  }

  return { type: condition.type, groupId: condition.groupId || null };
}

function buildAction(action: TriggerActionDraft): TriggerActionDefinition {
  const base = {
    id: action.id.trim(),
    type: action.type
  };

  if (action.type === "write_audit_entry") {
    return { ...base, message: normalizeOptionalText(action.message) };
  }

  if (action.type === "send_email") {
    return {
      ...base,
      to: parseRecipients(action.toText),
      subject: normalizeOptionalText(action.subject),
      body: normalizeOptionalText(action.body),
      ...(action.printTemplateId ? { printTemplateId: action.printTemplateId } : {})
    };
  }

  if (action.type === "change_status") {
    return { ...base, status: normalizeOptionalText(action.status) };
  }

  if (action.type === "update_field") {
    return { ...base, fieldId: action.fieldId?.trim() || null, value: normalizeRecordValue(action.value) };
  }

  if (action.type === "send_notification") {
    return {
      ...base,
      title: normalizeOptionalText(action.title),
      body: normalizeOptionalText(action.body),
      ...(action.recipientUserId ? { recipientUserIds: [action.recipientUserId] } : {}),
      ...(action.recipientGroupId ? { recipientGroupIds: [action.recipientGroupId] } : {})
    };
  }

  if (action.type === "create_record") {
    return {
      ...base,
      targetFormId: action.targetFormId || null,
      values: parseValueMappings(action.valueMappingsText) ?? {}
    };
  }

  if (action.type === "call_webhook") {
    return {
      ...base,
      webhookUrl: normalizeOptionalText(action.webhookUrl),
      webhookMethod: normalizeWebhookMethod(action.webhookMethod),
      webhookHeaders: parseWebhookHeaders(action.webhookHeadersText) ?? {}
    };
  }

  if (action.type === "start_workflow") {
    return { ...base, workflowDefinitionId: action.workflowDefinitionId || null };
  }

  return {
    ...base,
    ...(action.assignedToUserId ? { assignedToUserId: action.assignedToUserId } : {}),
    ...(action.assignedGroupId ? { assignedGroupId: action.assignedGroupId } : {})
  };
}

function normalizeRetryPolicy(policy: TriggerRetryPolicyDefinition | undefined): TriggerRetryPolicyDefinition {
  return {
    isEnabled: policy?.isEnabled ?? defaultTriggerRetryPolicy.isEnabled,
    maxAttempts: policy?.maxAttempts ?? defaultTriggerRetryPolicy.maxAttempts,
    delaySeconds: policy?.delaySeconds ?? defaultTriggerRetryPolicy.delaySeconds
  };
}

function normalizeRecordValue(value: FormRecordValue | undefined): FormRecordValue {
  return typeof value === "string" ? value.trim() : value ?? null;
}

function normalizeOptionalText(value?: string | null): string | null {
  const normalized = value?.trim();
  return normalized ? normalized : null;
}

function parseRecipients(value?: string): string[] {
  return (value ?? "")
    .split(/[,\n]/)
    .map((recipient) => recipient.trim())
    .filter((recipient, index, recipients) => recipient.length > 0 && recipients.indexOf(recipient) === index);
}

function parseValueMappings(value?: string): Record<string, TriggerActionValueDefinition> | null {
  if (!value?.trim()) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as unknown;

    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return null;
    }

    const mappings: Record<string, TriggerActionValueDefinition> = {};

    for (const [rawFieldId, rawMapping] of Object.entries(parsed as Record<string, unknown>)) {
      const fieldId = rawFieldId.trim();

      if (!fieldId || !rawMapping || typeof rawMapping !== "object" || Array.isArray(rawMapping)) {
        return null;
      }

      const mapping = rawMapping as Record<string, unknown>;
      const literal = mapping.literal;
      const hasSourceField = typeof mapping.sourceFieldId === "string" && mapping.sourceFieldId.trim().length > 0;
      const hasLiteral = Object.prototype.hasOwnProperty.call(mapping, "literal") && isFormRecordValue(literal) && !isBlank(literal);

      if (hasSourceField === hasLiteral) {
        return null;
      }

      if (hasSourceField) {
        mappings[fieldId] = { sourceFieldId: String(mapping.sourceFieldId).trim() };
      } else if (isFormRecordValue(literal)) {
        mappings[fieldId] = { literal: normalizeRecordValue(literal) };
      }
    }

    return Object.keys(mappings).length > 0 ? mappings : null;
  } catch {
    return null;
  }
}

function parseWebhookHeaders(value?: string): Record<string, string> | null {
  if (!value?.trim()) {
    return {};
  }

  try {
    const parsed = JSON.parse(value) as unknown;

    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return null;
    }

    const headers: Record<string, string> = {};

    for (const [key, rawValue] of Object.entries(parsed as Record<string, unknown>)) {
      const headerName = key.trim();

      if (!headerName || typeof rawValue !== "string") {
        return null;
      }

      headers[headerName] = rawValue.trim();
    }

    return headers;
  } catch {
    return null;
  }
}

function formatValueMappingsText(values?: Record<string, TriggerActionValueDefinition> | null): string {
  return values ? JSON.stringify(values, null, 2) : "";
}

function formatWebhookHeadersText(headers?: Record<string, string> | null): string {
  return headers ? JSON.stringify(headers, null, 2) : "";
}

export function isScheduledTriggerEvent(eventName: TriggerEventName): boolean {
  return eventName.startsWith("schedule.");
}

function scheduleKindFromEvent(eventName: TriggerEventName): TriggerScheduleDefinition["kind"] | "" {
  if (eventName === "schedule.once") return "once";
  if (eventName === "schedule.daily") return "daily";
  if (eventName === "schedule.weekly") return "weekly";
  if (eventName === "schedule.monthly") return "monthly";
  return "";
}

function normalizeWebhookMethod(value?: string): string {
  const normalized = value?.trim().toUpperCase();
  return normalized || "POST";
}

function isSupportedWebhookMethod(value?: string): boolean {
  return ["GET", "POST", "PUT", "PATCH", "DELETE"].includes(normalizeWebhookMethod(value));
}

function isHttpUrl(value?: string): boolean {
  if (!value?.trim()) {
    return false;
  }

  try {
    const url = new URL(value.trim());
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

function isFormRecordValue(value: unknown): value is FormRecordValue {
  return value === null || typeof value === "string" || typeof value === "number" || typeof value === "boolean";
}

function isBlank(value: FormRecordValue | undefined): boolean {
  return value === undefined || value === null || (typeof value === "string" && value.trim().length === 0);
}

function error(path: string, code: string, message: string): TriggerValidationError {
  return { path, code, message };
}

function sentenceCase(value: string): string {
  const normalized = value.replace(/[._-]+/g, " ").trim();
  return normalized ? `${normalized.charAt(0).toUpperCase()}${normalized.slice(1)}` : "Unknown";
}
