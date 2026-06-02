import type { EntityId } from "../../types/entities";
import type { FormField, FormRecordValue, FormSchema } from "../forms/types";
import {
  isTriggerActionType,
  isTriggerConditionType,
  isTriggerEventName,
  type CreateTriggerRequest,
  type TriggerActionDefinition,
  type TriggerActionType,
  type TriggerConditionDefinition,
  type TriggerConditionType,
  type TriggerDetail,
  type TriggerEventName,
  type TriggerExecutionStatus,
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
  { label: "Record assigned", value: "record.assigned" }
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
  { label: "Update field", value: "update_field" }
];

export function createEmptyTriggerDraft(formName = "Form"): TriggerDraft {
  return {
    name: `${formName.trim() || "Form"} automation`,
    description: "",
    eventName: "record.created",
    conditions: [],
    actions: [createTriggerActionDraft("write_audit_entry", 1)],
    isEnabled: true
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
    value: ""
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
    isEnabled: draft.isEnabled
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
    value: action.value ?? ""
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
      body: normalizeOptionalText(action.body)
    };
  }

  if (action.type === "change_status") {
    return { ...base, status: normalizeOptionalText(action.status) };
  }

  if (action.type === "update_field") {
    return { ...base, fieldId: action.fieldId?.trim() || null, value: normalizeRecordValue(action.value) };
  }

  return {
    ...base,
    ...(action.assignedToUserId ? { assignedToUserId: action.assignedToUserId } : {}),
    ...(action.assignedGroupId ? { assignedGroupId: action.assignedGroupId } : {})
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
