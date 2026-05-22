import type { CreateListReportRequest, ListReportDetail, ListReportSummary, ReportValidationError } from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type ReportsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class ReportsApiError extends Error {
  readonly errors: ReportValidationError[];

  constructor(message: string, errors: ReportValidationError[] = []) {
    super(message);
    this.name = "ReportsApiError";
    this.errors = errors;
  }
}

const defaultFetcher: ReportsFetcher = (input, init) => fetch(input, init);

export async function listReports(formId: string, fetcher: ReportsFetcher = defaultFetcher): Promise<ListReportSummary[]> {
  return requestItems<ListReportSummary>(
    `/api/forms/${encodeURIComponent(formId)}/reports`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function createListReport(
  formId: string,
  request: CreateListReportRequest,
  fetcher: ReportsFetcher = defaultFetcher
): Promise<ListReportDetail> {
  return requestJson<ListReportDetail>(
    `/api/forms/${encodeURIComponent(formId)}/reports`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: ReportsFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new ReportsApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: ReportsFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new ReportsApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
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

  return "Reports API request failed.";
}

function getValidationErrorsFromBody(body: unknown): ReportValidationError[] {
  if (!isRecord(body) || !Array.isArray(body.errors)) {
    return [];
  }

  return body.errors.filter(isReportValidationError);
}

function isReportValidationError(value: unknown): value is ReportValidationError {
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
