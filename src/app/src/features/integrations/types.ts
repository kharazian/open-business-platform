import type { AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";

export type IntegrationApiKeyScope =
  | "integrations.authenticate"
  | "integrations.records.read"
  | "integrations.records.create"
  | "integrations.webhooks.receive";

export type IntegrationApiKeyDto = AuditedEntityDto & ConcurrencyStampedDto & {
  name: string;
  integrationKey: string;
  keyPrefix: string;
  scopes: IntegrationApiKeyScope[];
  isActive: boolean;
  lastUsedAt?: string | null;
  lastUsedIp?: string | null;
  lastUsedUserAgent?: string | null;
  revokedAt?: string | null;
  revokedById?: EntityId | null;
};

export type CreateIntegrationApiKeyRequest = {
  name: string;
  integrationKey: string;
  scopes: IntegrationApiKeyScope[];
  isActive: boolean;
};

export type RevokeIntegrationApiKeyRequest = {
  reason?: string | null;
  concurrencyStamp: string;
};

export type RotateIntegrationApiKeyRequest = {
  concurrencyStamp: string;
};

export type IntegrationApiKeySecretResponse = {
  apiKey: IntegrationApiKeyDto;
  rawKey: string;
};

export type IntegrationLogDirection = "inbound" | "outbound";
export type IntegrationLogType = "api" | "webhook" | "import" | "export";
export type IntegrationLogStatus = "pending" | "running" | "succeeded" | "failed" | "canceled";

export type IntegrationLogDto = {
  id: EntityId;
  direction: IntegrationLogDirection;
  integrationType: IntegrationLogType;
  integrationKey: string;
  sourceType: string;
  sourceId?: EntityId | null;
  targetEntityType?: string | null;
  targetEntityId?: EntityId | null;
  status: IntegrationLogStatus;
  attemptCount: number;
  maxAttempts: number;
  isRetryable: boolean;
  retryNextAttemptAt?: string | null;
  retryLockedAt?: string | null;
  retryCompletedAt?: string | null;
  retryExhaustedAt?: string | null;
  retryRequestedAt?: string | null;
  retryRequestedById?: EntityId | null;
  requestMetadata?: unknown;
  responseMetadata?: unknown;
  errorCode?: string | null;
  errorMessage?: string | null;
  startedAt: string;
  completedAt?: string | null;
  createdAt: string;
};

export type RequestIntegrationLogRetryRequest = {
  reason?: string | null;
};

export type IntegrationLogFilters = {
  direction?: IntegrationLogDirection | "";
  status?: IntegrationLogStatus | "";
  type?: IntegrationLogType | "";
  source?: string;
  since?: string;
};

export type IntegrationConnectorType = "sftp" | "file_storage" | "vendor_api" | "webhook";

export type IntegrationConnectorDto = AuditedEntityDto & ConcurrencyStampedDto & {
  name: string;
  connectorKey: string;
  type: IntegrationConnectorType;
  config: Record<string, unknown>;
  configuredSecretNames: string[];
  isActive: boolean;
};

export type UpsertIntegrationConnectorRequest = {
  name: string;
  connectorKey: string;
  type: IntegrationConnectorType;
  config?: Record<string, unknown> | null;
  secrets?: Record<string, string | null> | null;
  isActive: boolean;
  concurrencyStamp?: string | null;
};

export type IncomingWebhookListenerAction = "create" | "upsert";
export type IncomingWebhookListenerAuthMode = "api_key" | "listener_secret";

export type IncomingWebhookFieldMappingDefinition = {
  sourcePath: string;
  targetFieldId: string;
  required?: boolean;
};

export type IncomingWebhookMappingDefinition = {
  fieldMappings: IncomingWebhookFieldMappingDefinition[];
};

export type UpsertIncomingWebhookListenerRequest = {
  name: string;
  listenerKey: string;
  targetFormId: EntityId;
  action: IncomingWebhookListenerAction;
  authMode: IncomingWebhookListenerAuthMode;
  mapping: IncomingWebhookMappingDefinition;
  isActive: boolean;
  safeLookupFieldId?: string | null;
};

export type IncomingWebhookListenerDto = AuditedEntityDto & ConcurrencyStampedDto & {
  name: string;
  listenerKey: string;
  targetFormId: EntityId;
  action: IncomingWebhookListenerAction;
  authMode: IncomingWebhookListenerAuthMode;
  secretPrefix?: string | null;
  safeLookupFieldId?: string | null;
  mapping: IncomingWebhookMappingDefinition;
  isActive: boolean;
};

export type IncomingWebhookListenerSecretResponse = {
  listener: IncomingWebhookListenerDto;
  rawSecret: string;
};

export type RecordImportJobStatus = "pending" | "running" | "succeeded" | "completed_with_errors" | "failed";

export type RecordImportFieldMappingDefinition = {
  csvHeader: string;
  targetFieldId: string;
};

export type RecordImportMappingDefinition = {
  fieldMappings: RecordImportFieldMappingDefinition[];
};

export type CreateRecordImportJobRequest = {
  formId: EntityId;
  integrationKey: string;
  fileName?: string | null;
  csvContent: string;
  mapping: RecordImportMappingDefinition;
};

export type RecordImportJobSummaryDto = {
  id: EntityId;
  formId: EntityId;
  integrationKey: string;
  fileName?: string | null;
  status: RecordImportJobStatus;
  totalRows: number;
  succeededRows: number;
  failedRows: number;
  startedAt: string;
  completedAt?: string | null;
  createdAt: string;
  createdById?: EntityId | null;
};

export type RecordImportJobDetailDto = RecordImportJobSummaryDto & ConcurrencyStampedDto & {
  mapping: RecordImportMappingDefinition;
  rows: Array<{
    id: EntityId;
    rowNumber: number;
    status: "succeeded" | "failed";
    recordId?: EntityId | null;
    errors: Array<{ fieldId?: string | null; code: string; message: string }>;
  }>;
  updatedAt?: string | null;
  updatedById?: EntityId | null;
};

export type ExternalExportJobSourceType = "form_records" | "list_report";
export type ExternalExportJobFormat = "csv" | "json";
export type ExternalExportJobStatus = "pending" | "running" | "succeeded" | "failed";

export type CreateExternalExportJobRequest = {
  sourceType: ExternalExportJobSourceType;
  format: ExternalExportJobFormat;
  integrationKey: string;
  formId?: EntityId | null;
  reportId?: EntityId | null;
  search?: string | null;
};

export type ExternalExportJobSummaryDto = {
  id: EntityId;
  sourceType: ExternalExportJobSourceType;
  format: ExternalExportJobFormat;
  integrationKey: string;
  formId?: EntityId | null;
  reportId?: EntityId | null;
  status: ExternalExportJobStatus;
  rowCount: number;
  artifactFileName?: string | null;
  artifactContentType?: string | null;
  artifactSizeBytes: number;
  startedAt: string;
  completedAt?: string | null;
  createdAt: string;
  createdById?: EntityId | null;
};

export type ExternalExportJobDetailDto = ExternalExportJobSummaryDto & ConcurrencyStampedDto & {
  artifactMetadata?: unknown;
  updatedAt?: string | null;
  updatedById?: EntityId | null;
};

export type ProcessingJobKind = "csv_record_import" | "record_export";
export type ProcessingScheduleKind = "once" | "daily" | "weekly" | "monthly";
export type ProcessingJobConfig = {
  formId: EntityId;
  integrationKey: string;
  sourceType?: ExternalExportJobSourceType | null;
  format?: ExternalExportJobFormat | null;
  reportId?: EntityId | null;
  search?: string | null;
  maxRows?: number;
  mapping?: RecordImportMappingDefinition | null;
};
export type ProcessingJobSchedule = {
  kind: ProcessingScheduleKind;
  timeZone: string;
  startAt: string;
  interval: number;
  dayOfWeek?: number | null;
  dayOfMonth?: number | null;
};
export type ProcessingJobRetryPolicy = { isEnabled: boolean; maxAttempts: number; delaySeconds: number };
export type ProcessingFailureNotificationPolicy = { isEnabled: boolean; includeOwner: boolean; recipientUserIds: EntityId[] };
export type ProcessingJobSummaryDto = {
  id: EntityId; name: string; kind: ProcessingJobKind; isEnabled: boolean; formId: EntityId;
  reportId?: EntityId | null; nextRunAt?: string | null; concurrencyStamp: string; createdAt: string; updatedAt?: string | null;
};
export type ProcessingJobDetailDto = ProcessingJobSummaryDto & {
  config: ProcessingJobConfig; schedule?: ProcessingJobSchedule | null; retryPolicy: ProcessingJobRetryPolicy;
  failureNotificationPolicy: ProcessingFailureNotificationPolicy; ownerUserId: EntityId;
};
export type ProcessingJobRunDto = {
  id: EntityId; definitionId: EntityId; source: "manual" | "scheduled" | "retry";
  status: "pending" | "running" | "succeeded" | "failed"; attempt: number; maxAttempts: number;
  nextAttemptAt: string; startedAt?: string | null; completedAt?: string | null; errorCode?: string | null;
  errorMessage?: string | null; inputFileName?: string | null; inputSizeBytes: number; inputChecksum?: string | null;
  recordImportJobId?: EntityId | null; externalExportJobId?: EntityId | null; retrySourceRunId?: EntityId | null; createdAt: string;
};
export type ProcessingPage<T> = { items: T[]; page: number; pageSize: number; totalCount: number };
export type CreateProcessingJobRequest = {
  name: string; kind: ProcessingJobKind; config: ProcessingJobConfig; schedule?: ProcessingJobSchedule | null;
  retryPolicy?: ProcessingJobRetryPolicy | null; isEnabled: boolean; failureNotificationPolicy?: ProcessingFailureNotificationPolicy | null;
};
export type UpdateProcessingJobRequest = Omit<CreateProcessingJobRequest, "kind" | "isEnabled"> & { concurrencyStamp: string };
export type ProcessingOperationalLogSeverity = "info" | "warning" | "error";
export type ProcessingOperationalLogDto = {
  id: EntityId; definitionId: EntityId; definitionName: string; runId?: EntityId | null; kind: ProcessingJobKind;
  severity: ProcessingOperationalLogSeverity; eventCode: string; message: string; attempt?: number | null;
  maxAttempts?: number | null; errorCode?: string | null; durationMilliseconds?: number | null;
  recordImportJobId?: EntityId | null; externalExportJobId?: EntityId | null; occurredAt: string;
};
export type ProcessingOperationsSummaryDto = {
  from: string; to: string; pending: number; running: number; succeeded: number; failed: number;
  retryScheduled: number; retryExhausted: number; scheduleSkipped: number; byKind: Record<string, number>;
};
export type ProcessingNotificationRecipientDto = { id: EntityId; name: string };
export type ProcessingOperationalLogFilters = {
  definitionId?: string; runId?: string; kind?: ProcessingJobKind | ""; severity?: ProcessingOperationalLogSeverity | "";
  eventCode?: string; errorCode?: string; from?: string; to?: string;
};

export const integrationApiKeyScopes: IntegrationApiKeyScope[] = [
  "integrations.authenticate",
  "integrations.records.read",
  "integrations.records.create",
  "integrations.webhooks.receive"
];

export const integrationConnectorTypes: IntegrationConnectorType[] = ["sftp", "file_storage", "vendor_api", "webhook"];
