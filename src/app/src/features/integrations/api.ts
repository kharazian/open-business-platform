import type {
  CreateIntegrationApiKeyRequest,
  IntegrationApiKeyDto,
  IntegrationApiKeySecretResponse,
  IntegrationLogDto,
  RequestIntegrationLogRetryRequest,
  RevokeIntegrationApiKeyRequest,
  RotateIntegrationApiKeyRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type IntegrationsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class IntegrationsApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "IntegrationsApiError";
  }
}

const defaultFetcher: IntegrationsFetcher = (input, init) => fetch(input, init);

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
