import type {
  CreateListReportRequest,
  ExecuteListReportOptions,
  ListReportDetail,
  ListReportExecution,
  ListReportSummary,
  ReportValidationError,
  UpdateListReportRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type ReportsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;
export type ReportCsvDownloader = (url: string) => void;
export type ListReportRuntimeOptions = Pick<ExecuteListReportOptions, "search" | "sortFieldId" | "sortDirection" | "filters">;

export class ReportsApiError extends Error {
  readonly errors: ReportValidationError[];

  constructor(message: string, errors: ReportValidationError[] = []) {
    super(message);
    this.name = "ReportsApiError";
    this.errors = errors;
  }
}

const defaultFetcher: ReportsFetcher = (input, init) => fetch(input, init);
const defaultCsvDownloader: ReportCsvDownloader = (url) => {
  window.location.assign(url);
};

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

export async function getListReport(
  formId: string,
  reportId: string,
  fetcher: ReportsFetcher = defaultFetcher
): Promise<ListReportDetail> {
  return requestJson<ListReportDetail>(
    `/api/forms/${encodeURIComponent(formId)}/reports/${encodeURIComponent(reportId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function updateListReport(
  formId: string,
  reportId: string,
  request: UpdateListReportRequest,
  fetcher: ReportsFetcher = defaultFetcher
): Promise<ListReportDetail> {
  return requestJson<ListReportDetail>(
    `/api/forms/${encodeURIComponent(formId)}/reports/${encodeURIComponent(reportId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function deleteListReport(
  formId: string,
  reportId: string,
  fetcher: ReportsFetcher = defaultFetcher
): Promise<void> {
  await requestVoid(
    `/api/forms/${encodeURIComponent(formId)}/reports/${encodeURIComponent(reportId)}`,
    { method: "DELETE", credentials: "include" },
    fetcher
  );
}

export async function executeListReport(
  formId: string,
  reportId: string,
  options: ExecuteListReportOptions = {},
  fetcher: ReportsFetcher = defaultFetcher
): Promise<ListReportExecution> {
  const query = new URLSearchParams();

  if (options.page !== undefined) {
    query.set("page", String(options.page));
  }

  if (options.pageSize !== undefined) {
    query.set("pageSize", String(options.pageSize));
  }

  appendListReportRuntimeQuery(query, options);

  const queryString = query.toString();

  return requestJson<ListReportExecution>(
    `/api/forms/${encodeURIComponent(formId)}/reports/${encodeURIComponent(reportId)}/run${queryString ? `?${queryString}` : ""}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export function getListReportCsvExportUrl(
  formId: string,
  reportId: string,
  options: ListReportRuntimeOptions = {}
): string {
  const query = new URLSearchParams();
  appendListReportRuntimeQuery(query, options);

  const queryString = query.toString();
  return `/api/forms/${encodeURIComponent(formId)}/reports/${encodeURIComponent(reportId)}/export.csv${queryString ? `?${queryString}` : ""}`;
}

export function downloadListReportCsv(
  formId: string,
  reportId: string,
  options: ListReportRuntimeOptions = {},
  downloader: ReportCsvDownloader = defaultCsvDownloader
): void {
  downloader(getListReportCsvExportUrl(formId, reportId, options));
}

function appendListReportRuntimeQuery(query: URLSearchParams, options: ListReportRuntimeOptions): void {
  if (options.search?.trim()) {
    query.set("search", options.search.trim());
  }

  if (options.sortFieldId?.trim()) {
    query.set("sortFieldId", options.sortFieldId.trim());
    query.set("sortDirection", options.sortDirection === "asc" ? "asc" : "desc");
  }

  Object.entries(options.filters ?? {}).forEach(([fieldId, value]) => {
    if (!value?.trim()) {
      return;
    }

    query.set(`filter.${fieldId}`, value.trim());
  });
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

async function requestVoid(input: string, init: RequestInit, fetcher: ReportsFetcher): Promise<void> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new ReportsApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
  }
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
