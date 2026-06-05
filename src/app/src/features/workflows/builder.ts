import type { EntityId } from "../../types/entities";
import type {
  CreateWorkflowRequest,
  WorkflowActionDefinition,
  WorkflowActionType,
  WorkflowDefinitionConfig,
  WorkflowDetail,
  WorkflowSummary,
  WorkflowValidationError
} from "./types";

export type WorkflowStatusTone = "default" | "success" | "warning" | "danger";

export type WorkflowDraft = {
  id?: EntityId;
  formId?: EntityId;
  name: string;
  description: string;
  configText: string;
  isEnabled: boolean;
  concurrencyStamp?: string;
};

export type WorkflowDraftValidationResult = {
  valid: boolean;
  errors: WorkflowValidationError[];
};

export const workflowActionOptions: Array<{ label: string; value: WorkflowActionType }> = [
  { label: "Write audit entry", value: "write_audit_entry" },
  { label: "Send email", value: "send_email" },
  { label: "Assign record", value: "assign_record" },
  { label: "Update field", value: "update_field" },
  { label: "Send notification", value: "send_notification" },
  { label: "Create record", value: "create_record" }
];

export const defaultWorkflowConfig: WorkflowDefinitionConfig = {
  schemaVersion: 1,
  initialStateKey: "draft",
  states: [
    { key: "draft", name: "Draft" },
    { key: "submitted", name: "Submitted" },
    { key: "approved", name: "Approved", isFinal: true }
  ],
  transitions: [
    { key: "submit", name: "Submit", fromStateKey: "draft", toStateKey: "submitted" },
    { key: "approve", name: "Approve", fromStateKey: "submitted", toStateKey: "approved" }
  ],
  approvalSteps: []
};

export function createEmptyWorkflowDraft(formName = "Form"): WorkflowDraft {
  return {
    name: `${formName.trim() || "Form"} workflow`,
    description: "",
    configText: formatWorkflowConfigText(defaultWorkflowConfig),
    isEnabled: true
  };
}

export function createWorkflowDraftFromDetail(detail: WorkflowDetail): WorkflowDraft {
  return {
    id: detail.id,
    formId: detail.formId,
    name: detail.name,
    description: detail.description ?? "",
    configText: formatWorkflowConfigText(detail.config),
    isEnabled: detail.isEnabled,
    concurrencyStamp: detail.concurrencyStamp
  };
}

export function createWorkflowAction(type: WorkflowActionType = "write_audit_entry", index = Date.now()): WorkflowActionDefinition {
  return {
    id: `action-${index}`,
    type,
    message: type === "write_audit_entry" ? "Workflow transition completed." : "",
    to: type === "send_email" ? [] : null,
    subject: type === "send_email" ? "Workflow update" : null,
    body: type === "send_email" || type === "send_notification" ? "A workflow transition completed." : null,
    assignedToUserId: null,
    assignedGroupId: null,
    fieldId: type === "update_field" ? "" : null,
    value: type === "update_field" ? "" : undefined,
    title: type === "send_notification" ? "Workflow update" : null,
    recipientUserIds: type === "send_notification" ? [] : null,
    recipientGroupIds: type === "send_notification" ? [] : null,
    targetFormId: type === "create_record" ? "" : null,
    values: type === "create_record" ? { field_id: { literal: "value" } } : null
  };
}

export function buildWorkflowRequest(draft: WorkflowDraft): CreateWorkflowRequest {
  return {
    name: draft.name.trim(),
    description: normalizeOptionalText(draft.description),
    config: parseWorkflowConfig(draft.configText) ?? defaultWorkflowConfig,
    isEnabled: draft.isEnabled
  };
}

export function validateWorkflowDraft(draft: WorkflowDraft): WorkflowDraftValidationResult {
  const errors: WorkflowValidationError[] = [];
  const config = parseWorkflowConfig(draft.configText);

  if (!draft.name.trim()) {
    errors.push(error("name", "workflow.name.required", "Workflow name is required."));
  }

  if (!config) {
    errors.push(error("config", "workflow.config.invalid_json", "Enter valid workflow config JSON."));
  }

  return { valid: errors.length === 0, errors };
}

export function formatWorkflowStatus(workflow: Pick<WorkflowSummary, "status" | "isEnabled" | "hasUnpublishedChanges">): { label: string; tone: WorkflowStatusTone } {
  if (!workflow.isEnabled) {
    return { label: "Disabled", tone: "default" };
  }

  if (workflow.status === "published" && workflow.hasUnpublishedChanges) {
    return { label: "Published changes", tone: "warning" };
  }

  if (workflow.status === "published") {
    return { label: "Published", tone: "success" };
  }

  if (workflow.status === "draft") {
    return { label: "Draft", tone: "warning" };
  }

  return { label: sentenceCase(workflow.status), tone: "default" };
}

export function formatWorkflowConfigText(config: WorkflowDefinitionConfig): string {
  return JSON.stringify(config, null, 2);
}

export function parseWorkflowConfig(value: string): WorkflowDefinitionConfig | null {
  if (!value.trim()) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as unknown;
    return isWorkflowDefinitionConfig(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

export function formatWorkflowDate(value?: string | null): string {
  if (!value) {
    return "Not yet";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function isWorkflowDefinitionConfig(value: unknown): value is WorkflowDefinitionConfig {
  if (!isRecord(value)) {
    return false;
  }

  return (
    value.schemaVersion === 1
    && typeof value.initialStateKey === "string"
    && Array.isArray(value.states)
    && Array.isArray(value.transitions)
    && Array.isArray(value.approvalSteps)
  );
}

function sentenceCase(value: string): string {
  return value
    .replace(/[_-]+/g, " ")
    .replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function normalizeOptionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized.length === 0 ? null : normalized;
}

function error(path: string, code: string, message: string): WorkflowValidationError {
  return { path, code, message };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
