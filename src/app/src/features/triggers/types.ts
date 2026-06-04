import type { AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";
import type { FormRecordValue } from "../forms/types";

export const triggerEvents = [
  "record.created",
  "record.updated",
  "field.changed",
  "status.changed",
  "record.assigned",
  "schedule.once",
  "schedule.daily",
  "schedule.weekly",
  "schedule.monthly"
] as const;
export const triggerConditionModes = ["all"] as const;
export const triggerConditionTypes = [
  "field_equals",
  "field_changed",
  "status_changed_to",
  "department_equals",
  "assigned_to_user",
  "assigned_to_group"
] as const;
export const triggerActionTypes = [
  "write_audit_entry",
  "send_email",
  "change_status",
  "assign_record",
  "update_field",
  "send_notification",
  "create_record",
  "call_webhook"
] as const;
export const triggerExecutionStatuses = ["success", "failed", "skipped"] as const;
export const triggerScheduleKinds = ["once", "daily", "weekly", "monthly"] as const;

export type TriggerEventName = (typeof triggerEvents)[number];
export type TriggerConditionMode = (typeof triggerConditionModes)[number];
export type TriggerConditionType = (typeof triggerConditionTypes)[number];
export type TriggerActionType = (typeof triggerActionTypes)[number];
export type TriggerExecutionStatus = (typeof triggerExecutionStatuses)[number];
export type TriggerScheduleKind = (typeof triggerScheduleKinds)[number];

export type TriggerRetryPolicyDefinition = {
  isEnabled: boolean;
  maxAttempts: number;
  delaySeconds: number;
};

export type TriggerScheduleDefinition = {
  kind: TriggerScheduleKind;
  timeZone: string;
  startAt: string;
};

export type TriggerConditionDefinition = {
  type: TriggerConditionType;
  fieldId?: string | null;
  value?: FormRecordValue;
  status?: string | null;
  departmentId?: EntityId | null;
  userId?: EntityId | null;
  groupId?: EntityId | null;
};

export type TriggerConditionGroupDefinition = {
  mode: TriggerConditionMode;
  conditions: TriggerConditionDefinition[];
};

export type TriggerActionDefinition = {
  id: string;
  type: TriggerActionType;
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
  values?: Record<string, TriggerActionValueDefinition> | null;
  webhookUrl?: string | null;
  webhookMethod?: string | null;
  webhookHeaders?: Record<string, string> | null;
  webhookBody?: unknown;
};

export type TriggerActionValueDefinition = {
  literal?: FormRecordValue;
  sourceFieldId?: string | null;
};

export type CreateTriggerRequest = {
  name: string;
  description?: string | null;
  eventName: TriggerEventName;
  conditions: TriggerConditionGroupDefinition;
  actions: TriggerActionDefinition[];
  isEnabled: boolean;
  retryPolicy: TriggerRetryPolicyDefinition;
  schedule?: TriggerScheduleDefinition | null;
};

export type UpdateTriggerRequest = CreateTriggerRequest & {
  concurrencyStamp: string;
};

export interface TriggerSummary extends AuditedEntityDto, ConcurrencyStampedDto {
  formId: EntityId;
  name: string;
  description?: string | null;
  eventName: TriggerEventName;
  isEnabled: boolean;
  conditionCount: number;
  actionCount: number;
  retryPolicy: TriggerRetryPolicyDefinition;
  schedule?: TriggerScheduleDefinition | null;
  scheduleNextRunAt?: string | null;
  scheduleLastRunAt?: string | null;
}

export interface TriggerDetail extends AuditedEntityDto, ConcurrencyStampedDto {
  formId: EntityId;
  name: string;
  description?: string | null;
  eventName: TriggerEventName;
  conditions: TriggerConditionGroupDefinition;
  actions: TriggerActionDefinition[];
  isEnabled: boolean;
  retryPolicy: TriggerRetryPolicyDefinition;
  schedule?: TriggerScheduleDefinition | null;
  scheduleNextRunAt?: string | null;
  scheduleLastRunAt?: string | null;
}

export type TriggerExecutionLog = {
  id: EntityId;
  triggerId: EntityId;
  formId: EntityId;
  eventName: TriggerEventName;
  entityType: string;
  entityId: EntityId;
  status: TriggerExecutionStatus;
  retryOfLogId?: EntityId | null;
  retryState?: "pending" | "completed" | "exhausted" | "disabled" | null;
  autoRetryAttemptCount?: number;
  autoRetryMaxAttempts?: number;
  autoRetryNextAttemptAt?: string | null;
  input?: unknown;
  result?: unknown;
  errorMessage?: string | null;
  startedAt: string;
  completedAt?: string | null;
  createdAt: string;
};

export type TriggerValidationError = {
  path: string;
  code: string;
  message: string;
};

export function isTriggerEventName(value: string): value is TriggerEventName {
  return (triggerEvents as readonly string[]).includes(value);
}

export function isTriggerConditionType(value: string): value is TriggerConditionType {
  return (triggerConditionTypes as readonly string[]).includes(value);
}

export function isTriggerActionType(value: string): value is TriggerActionType {
  return (triggerActionTypes as readonly string[]).includes(value);
}
