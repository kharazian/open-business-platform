import type { AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";
import type { FormRecordValue } from "../forms/types";

export const workflowStatuses = ["draft", "published"] as const;
export const workflowApprovalModes = ["any", "all"] as const;
export const workflowAssigneeRuleTypes = ["user", "group", "department_manager", "record_owner"] as const;
export const workflowActionTypes = ["write_audit_entry", "send_email", "assign_record", "update_field", "send_notification", "create_record"] as const;

export type WorkflowStatus = (typeof workflowStatuses)[number];
export type WorkflowApprovalMode = (typeof workflowApprovalModes)[number];
export type WorkflowAssigneeRuleType = (typeof workflowAssigneeRuleTypes)[number];
export type WorkflowActionType = (typeof workflowActionTypes)[number];

export type WorkflowStateDefinition = {
  key: string;
  name: string;
  isFinal?: boolean;
};

export type WorkflowTransitionDefinition = {
  key: string;
  name: string;
  fromStateKey: string;
  toStateKey: string;
  approvalStepKey?: string | null;
  actions?: WorkflowActionDefinition[] | null;
};

export type WorkflowApprovalStepDefinition = {
  key: string;
  name: string;
  mode: WorkflowApprovalMode;
  assigneeRules: WorkflowAssigneeRuleDefinition[];
};

export type WorkflowAssigneeRuleDefinition = {
  type: WorkflowAssigneeRuleType;
  userId?: EntityId | null;
  groupId?: EntityId | null;
  departmentId?: EntityId | null;
};

export type WorkflowActionDefinition = {
  id: string;
  type: WorkflowActionType;
  message?: string | null;
  to?: string[] | null;
  subject?: string | null;
  body?: string | null;
  status?: string | null;
  assignedToUserId?: EntityId | null;
  assignedGroupId?: EntityId | null;
  fieldId?: string | null;
  value?: FormRecordValue;
  title?: string | null;
  recipientUserIds?: EntityId[] | null;
  recipientGroupIds?: EntityId[] | null;
  targetFormId?: EntityId | null;
  values?: Record<string, WorkflowActionValueDefinition> | null;
};

export type WorkflowActionValueDefinition = {
  literal?: FormRecordValue;
  sourceFieldId?: string | null;
};

export type WorkflowDefinitionConfig = {
  schemaVersion: 1;
  initialStateKey: string;
  states: WorkflowStateDefinition[];
  transitions: WorkflowTransitionDefinition[];
  approvalSteps: WorkflowApprovalStepDefinition[];
};

export type CreateWorkflowRequest = {
  name: string;
  description?: string | null;
  config: WorkflowDefinitionConfig;
  isEnabled: boolean;
};

export type UpdateWorkflowRequest = CreateWorkflowRequest & {
  concurrencyStamp: string;
};

export type StartRecordWorkflowRequest = {
  workflowDefinitionId: EntityId;
  concurrencyStamp: string;
};

export type ExecuteRecordWorkflowTransitionRequest = {
  concurrencyStamp: string;
};

export type RespondWorkflowApprovalRequest = {
  comment?: string | null;
};

export interface WorkflowSummary extends AuditedEntityDto, ConcurrencyStampedDto {
  formId: EntityId;
  name: string;
  description?: string | null;
  status: WorkflowStatus;
  isEnabled: boolean;
  hasUnpublishedChanges: boolean;
  currentVersionId?: EntityId | null;
  currentVersionNumber?: number | null;
  stateCount: number;
  transitionCount: number;
  approvalStepCount: number;
}

export interface WorkflowDetail extends AuditedEntityDto, ConcurrencyStampedDto {
  formId: EntityId;
  name: string;
  description?: string | null;
  status: WorkflowStatus;
  isEnabled: boolean;
  hasUnpublishedChanges: boolean;
  currentVersionId?: EntityId | null;
  currentVersionNumber?: number | null;
  config: WorkflowDefinitionConfig;
}

export type RecordWorkflowTransition = {
  key: string;
  name: string;
  fromStateKey: string;
  toStateKey: string;
  requiresApproval: boolean;
};

export type RecordWorkflowStartOption = {
  workflowDefinitionId: EntityId;
  name: string;
  currentVersionNumber: number;
  initialStateKey: string;
};

export type RecordWorkflowHistory = {
  id: EntityId;
  workflowDefinitionId: EntityId;
  workflowDefinitionVersionId: EntityId;
  recordId: EntityId;
  fromStateKey?: string | null;
  toStateKey: string;
  transitionKey?: string | null;
  action: string;
  actorUserId?: EntityId | null;
  createdAt: string;
};

export type RecordWorkflowState = {
  recordId: EntityId;
  formId: EntityId;
  workflowDefinitionId?: EntityId | null;
  workflowDefinitionVersionId?: EntityId | null;
  workflowName?: string | null;
  workflowVersionNumber?: number | null;
  stateKey?: string | null;
  availableWorkflows: RecordWorkflowStartOption[];
  availableTransitions: RecordWorkflowTransition[];
  history: RecordWorkflowHistory[];
  recordConcurrencyStamp: string;
};

export type WorkflowApprovalTaskStatus = "pending" | "approved" | "rejected" | "canceled";

export type WorkflowApprovalTask = {
  id: EntityId;
  approvalGroupId: EntityId;
  workflowDefinitionId: EntityId;
  workflowDefinitionVersionId: EntityId;
  formId: EntityId;
  recordId: EntityId;
  approvalStepKey: string;
  approvalStepName: string;
  mode: WorkflowApprovalMode;
  transitionKey: string;
  transitionName: string;
  fromStateKey: string;
  toStateKey: string;
  status: WorkflowApprovalTaskStatus;
  assignedToUserId: EntityId;
  requestedById?: EntityId | null;
  respondedById?: EntityId | null;
  respondedAt?: string | null;
  comment?: string | null;
  createdAt: string;
};

export type WorkflowValidationError = {
  path: string;
  code: string;
  message: string;
};

export function isWorkflowStatus(value: string): value is WorkflowStatus {
  return (workflowStatuses as readonly string[]).includes(value);
}
