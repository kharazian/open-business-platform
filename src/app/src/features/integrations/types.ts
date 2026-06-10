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

export const integrationApiKeyScopes: IntegrationApiKeyScope[] = [
  "integrations.authenticate",
  "integrations.records.read",
  "integrations.records.create",
  "integrations.webhooks.receive"
];
