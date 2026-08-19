import type {
  CreateIntegrationApiKeyRequest,
  CreateExternalExportJobRequest,
  IntegrationConnectorDto,
  CreateRecordImportJobRequest,
  ExternalExportJobDetailDto,
  ExternalExportJobSummaryDto,
  IncomingWebhookListenerDto,
  IncomingWebhookListenerSecretResponse,
  IntegrationApiKeyDto,
  IntegrationApiKeySecretResponse,
  IntegrationLogDto,
  RequestIntegrationLogRetryRequest,
  RecordImportJobDetailDto,
  RecordImportJobSummaryDto,
  RevokeIntegrationApiKeyRequest,
  RotateIntegrationApiKeyRequest,
  UpsertIntegrationConnectorRequest,
  UpsertIncomingWebhookListenerRequest,
  CreateProcessingJobRequest,
  ProcessingJobDetailDto,
  ProcessingJobRunDto,
  ProcessingJobSummaryDto,
  ProcessingPage,
  UpdateProcessingJobRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

type ApiBlobResponse = {
  ok: boolean;
  status?: number;
  blob: () => Promise<Blob>;
};

export type IntegrationsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;
export type IntegrationsBinaryFetcher = (input: string, init?: RequestInit) => Promise<ApiBlobResponse>;

export class IntegrationsApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "IntegrationsApiError";
  }
}

const defaultFetcher: IntegrationsFetcher = (input, init) => fetch(input, init);
const defaultBinaryFetcher: IntegrationsBinaryFetcher = (input, init) => fetch(input, init);

export async function listIntegrationApiKeys(fetcher: IntegrationsFetcher = defaultFetcher): Promise<IntegrationApiKeyDto[]> {
  return requestItems<IntegrationApiKeyDto>("/api/integrations/api-keys", { method: "GET", credentials: "include" }, fetcher);
}

export async function createIntegrationApiKey(
  request: CreateIntegrationApiKeyRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IntegrationApiKeySecretResponse> {
  return requestJson<IntegrationApiKeySecretResponse>(
    "/api/integrations/api-keys",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function revokeIntegrationApiKey(
  apiKeyId: string,
  request: RevokeIntegrationApiKeyRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IntegrationApiKeyDto> {
  return requestJson<IntegrationApiKeyDto>(
    `/api/integrations/api-keys/${encodeURIComponent(apiKeyId)}/revoke`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function rotateIntegrationApiKey(
  apiKeyId: string,
  request: RotateIntegrationApiKeyRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IntegrationApiKeySecretResponse> {
  return requestJson<IntegrationApiKeySecretResponse>(
    `/api/integrations/api-keys/${encodeURIComponent(apiKeyId)}/rotate`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listIntegrationConnectors(fetcher: IntegrationsFetcher = defaultFetcher): Promise<IntegrationConnectorDto[]> {
  return requestItems<IntegrationConnectorDto>("/api/integrations/connectors", { method: "GET", credentials: "include" }, fetcher);
}

export async function createIntegrationConnector(
  request: UpsertIntegrationConnectorRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IntegrationConnectorDto> {
  return requestJson<IntegrationConnectorDto>(
    "/api/integrations/connectors",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateIntegrationConnector(
  connectorId: string,
  request: UpsertIntegrationConnectorRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IntegrationConnectorDto> {
  return requestJson<IntegrationConnectorDto>(
    `/api/integrations/connectors/${encodeURIComponent(connectorId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listIntegrationLogs(fetcher: IntegrationsFetcher = defaultFetcher): Promise<IntegrationLogDto[]> {
  return requestItems<IntegrationLogDto>("/api/integrations/logs", { method: "GET", credentials: "include" }, fetcher);
}

export async function requestIntegrationLogRetry(
  logId: string,
  request: RequestIntegrationLogRetryRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IntegrationLogDto> {
  return requestJson<IntegrationLogDto>(
    `/api/integrations/logs/${encodeURIComponent(logId)}/retry-request`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listIncomingWebhookListeners(fetcher: IntegrationsFetcher = defaultFetcher): Promise<IncomingWebhookListenerDto[]> {
  return requestItems<IncomingWebhookListenerDto>("/api/integrations/webhooks", { method: "GET", credentials: "include" }, fetcher);
}

export async function createIncomingWebhookListener(
  request: UpsertIncomingWebhookListenerRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IncomingWebhookListenerSecretResponse> {
  return requestJson<IncomingWebhookListenerSecretResponse>(
    "/api/integrations/webhooks",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateIncomingWebhookListener(
  listenerId: string,
  request: UpsertIncomingWebhookListenerRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IncomingWebhookListenerDto> {
  return requestJson<IncomingWebhookListenerDto>(
    `/api/integrations/webhooks/${encodeURIComponent(listenerId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function rotateIncomingWebhookListenerSecret(
  listenerId: string,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<IncomingWebhookListenerSecretResponse> {
  return requestJson<IncomingWebhookListenerSecretResponse>(
    `/api/integrations/webhooks/${encodeURIComponent(listenerId)}/rotate-secret`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" }
    },
    fetcher
  );
}

export async function listRecordImportJobs(fetcher: IntegrationsFetcher = defaultFetcher): Promise<RecordImportJobSummaryDto[]> {
  return requestItems<RecordImportJobSummaryDto>("/api/integrations/imports", { method: "GET", credentials: "include" }, fetcher);
}

export async function createRecordImportJob(
  request: CreateRecordImportJobRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<RecordImportJobDetailDto> {
  return requestJson<RecordImportJobDetailDto>(
    "/api/integrations/imports",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listExternalExportJobs(fetcher: IntegrationsFetcher = defaultFetcher): Promise<ExternalExportJobSummaryDto[]> {
  return requestItems<ExternalExportJobSummaryDto>("/api/integrations/exports", { method: "GET", credentials: "include" }, fetcher);
}

export async function createExternalExportJob(
  request: CreateExternalExportJobRequest,
  fetcher: IntegrationsFetcher = defaultFetcher
): Promise<ExternalExportJobDetailDto> {
  return requestJson<ExternalExportJobDetailDto>(
    "/api/integrations/exports",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function downloadExternalExportArtifact(
  exportJobId: string,
  fetcher: IntegrationsBinaryFetcher = defaultBinaryFetcher
): Promise<Blob> {
  const response = await fetcher(
    `/api/integrations/exports/${encodeURIComponent(exportJobId)}/artifact`,
    { method: "GET", credentials: "include" }
  );

  if (!response.ok) {
    throw new IntegrationsApiError("Export artifact download failed.");
  }

  return await response.blob();
}

export function listProcessingJobs(page = 1, pageSize = 25, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingPage<ProcessingJobSummaryDto>> {
  return requestJson(`/api/processing-jobs?page=${page}&pageSize=${pageSize}`, { method: "GET", credentials: "include" }, fetcher);
}

export function createProcessingJob(request: CreateProcessingJobRequest, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingJobDetailDto> {
  return requestJson("/api/processing-jobs", { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) }, fetcher);
}

export function getProcessingJob(jobId: string, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingJobDetailDto> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(jobId)}`, { method: "GET", credentials: "include" }, fetcher);
}

export function updateProcessingJob(jobId: string, request: UpdateProcessingJobRequest, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingJobDetailDto> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(jobId)}`, { method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) }, fetcher);
}

export function setProcessingJobEnabled(job: ProcessingJobSummaryDto, enabled: boolean, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingJobDetailDto> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(job.id)}/${enabled ? "enable" : "disable"}`, { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ concurrencyStamp: job.concurrencyStamp }) }, fetcher);
}

export function deleteProcessingJob(job: ProcessingJobSummaryDto, fetcher: IntegrationsFetcher = defaultFetcher): Promise<void> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(job.id)}`, { method: "DELETE", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ concurrencyStamp: job.concurrencyStamp }) }, fetcher);
}

export function listProcessingJobRuns(jobId: string, page = 1, pageSize = 25, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingPage<ProcessingJobRunDto>> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(jobId)}/runs?page=${page}&pageSize=${pageSize}`, { method: "GET", credentials: "include" }, fetcher);
}

export function queueProcessingJob(jobId: string, fileName?: string | null, csvContent?: string | null, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingJobRunDto> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(jobId)}/runs`, { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ fileName, csvContent }) }, fetcher);
}

export function retryProcessingJobRun(jobId: string, runId: string, fetcher: IntegrationsFetcher = defaultFetcher): Promise<ProcessingJobRunDto> {
  return requestJson(`/api/processing-jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(runId)}/retry`, { method: "POST", credentials: "include" }, fetcher);
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: IntegrationsFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new IntegrationsApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: IntegrationsFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new IntegrationsApiError(getErrorMessageFromBody(body));
  }

  return body as T;
}

async function readJson(response: ApiFetchResponse): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getErrorMessageFromBody(body: unknown): string {
  if (isRecord(body) && typeof body.message === "string" && body.message.trim().length > 0) {
    return body.message;
  }

  return "Integrations API request failed.";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
