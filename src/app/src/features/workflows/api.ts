import type {
  CreateWorkflowRequest,
  UpdateWorkflowRequest,
  WorkflowDetail,
  WorkflowSummary,
  WorkflowValidationError
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type WorkflowsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class WorkflowsApiError extends Error {
  readonly errors: WorkflowValidationError[];

  constructor(message: string, errors: WorkflowValidationError[] = []) {
    super(message);
    this.name = "WorkflowsApiError";
    this.errors = errors;
  }
}

const defaultFetcher: WorkflowsFetcher = (input, init) => fetch(input, init);

export async function listWorkflows(formId: string, fetcher: WorkflowsFetcher = defaultFetcher): Promise<WorkflowSummary[]> {
  return requestItems<WorkflowSummary>(
    `/api/forms/${encodeURIComponent(formId)}/workflows`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function createWorkflow(
  formId: string,
  request: CreateWorkflowRequest,
  fetcher: WorkflowsFetcher = defaultFetcher
): Promise<WorkflowDetail> {
  return requestJson<WorkflowDetail>(
    `/api/forms/${encodeURIComponent(formId)}/workflows`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function getWorkflow(workflowId: string, fetcher: WorkflowsFetcher = defaultFetcher): Promise<WorkflowDetail> {
  return requestJson<WorkflowDetail>(
    `/api/workflows/${encodeURIComponent(workflowId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function updateWorkflow(
  workflowId: string,
  request: UpdateWorkflowRequest,
  fetcher: WorkflowsFetcher = defaultFetcher
): Promise<WorkflowDetail> {
  return requestJson<WorkflowDetail>(
    `/api/workflows/${encodeURIComponent(workflowId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function publishWorkflow(workflowId: string, concurrencyStamp: string, fetcher: WorkflowsFetcher = defaultFetcher): Promise<WorkflowDetail> {
  return postWorkflowStateChange(workflowId, "publish", concurrencyStamp, fetcher);
}

export async function enableWorkflow(workflowId: string, concurrencyStamp: string, fetcher: WorkflowsFetcher = defaultFetcher): Promise<WorkflowDetail> {
  return postWorkflowStateChange(workflowId, "enable", concurrencyStamp, fetcher);
}

export async function disableWorkflow(workflowId: string, concurrencyStamp: string, fetcher: WorkflowsFetcher = defaultFetcher): Promise<WorkflowDetail> {
  return postWorkflowStateChange(workflowId, "disable", concurrencyStamp, fetcher);
}

async function postWorkflowStateChange(
  workflowId: string,
  action: "publish" | "enable" | "disable",
  concurrencyStamp: string,
  fetcher: WorkflowsFetcher
): Promise<WorkflowDetail> {
  return requestJson<WorkflowDetail>(
    `/api/workflows/${encodeURIComponent(workflowId)}/${action}`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ concurrencyStamp })
    },
    fetcher
  );
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: WorkflowsFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new WorkflowsApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: WorkflowsFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new WorkflowsApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
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

  return "Workflows API request failed.";
}

function getValidationErrorsFromBody(body: unknown): WorkflowValidationError[] {
  if (!isRecord(body) || !Array.isArray(body.errors)) {
    return [];
  }

  return body.errors.filter(isWorkflowValidationError);
}

function isWorkflowValidationError(value: unknown): value is WorkflowValidationError {
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

