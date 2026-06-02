import type {
  CreateTriggerRequest,
  TriggerDetail,
  TriggerExecutionLog,
  TriggerSummary,
  TriggerValidationError,
  UpdateTriggerRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type TriggersFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class TriggersApiError extends Error {
  readonly errors: TriggerValidationError[];

  constructor(message: string, errors: TriggerValidationError[] = []) {
    super(message);
    this.name = "TriggersApiError";
    this.errors = errors;
  }
}

const defaultFetcher: TriggersFetcher = (input, init) => fetch(input, init);

export async function listTriggers(formId: string, fetcher: TriggersFetcher = defaultFetcher): Promise<TriggerSummary[]> {
  return requestItems<TriggerSummary>(
    `/api/forms/${encodeURIComponent(formId)}/triggers`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function createTrigger(
  formId: string,
  request: CreateTriggerRequest,
  fetcher: TriggersFetcher = defaultFetcher
): Promise<TriggerDetail> {
  return requestJson<TriggerDetail>(
    `/api/forms/${encodeURIComponent(formId)}/triggers`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function getTrigger(triggerId: string, fetcher: TriggersFetcher = defaultFetcher): Promise<TriggerDetail> {
  return requestJson<TriggerDetail>(
    `/api/triggers/${encodeURIComponent(triggerId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function updateTrigger(
  triggerId: string,
  request: UpdateTriggerRequest,
  fetcher: TriggersFetcher = defaultFetcher
): Promise<TriggerDetail> {
  return requestJson<TriggerDetail>(
    `/api/triggers/${encodeURIComponent(triggerId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listTriggerLogs(triggerId: string, fetcher: TriggersFetcher = defaultFetcher): Promise<TriggerExecutionLog[]> {
  return requestItems<TriggerExecutionLog>(
    `/api/triggers/${encodeURIComponent(triggerId)}/logs`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function retryTriggerLog(
  triggerId: string,
  logId: string,
  fetcher: TriggersFetcher = defaultFetcher
): Promise<TriggerExecutionLog> {
  return requestJson<TriggerExecutionLog>(
    `/api/triggers/${encodeURIComponent(triggerId)}/logs/${encodeURIComponent(logId)}/retry`,
    { method: "POST", credentials: "include" },
    fetcher
  );
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: TriggersFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new TriggersApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: TriggersFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new TriggersApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
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

  return "Triggers API request failed.";
}

function getValidationErrorsFromBody(body: unknown): TriggerValidationError[] {
  if (!isRecord(body) || !Array.isArray(body.errors)) {
    return [];
  }

  return body.errors.filter(isTriggerValidationError);
}

function isTriggerValidationError(value: unknown): value is TriggerValidationError {
  return (
    isRecord(value)
    && typeof value.path === "string"
    && typeof value.code === "string"
    && typeof value.message === "string"
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
