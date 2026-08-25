import type {
  ChartWidgetConfig,
  ChartWidgetPreview,
  ArchivedDashboard,
  CreateDashboardRequest,
  DashboardAnalyticsRequest,
  DashboardAnalyticsResponse,
  DashboardDetail,
  DashboardNavigationItem,
  DashboardPublishedComparison,
  DashboardRevisionSummary,
  DashboardSharingOptions,
  DashboardSharingSettings,
  DashboardSummary,
  DashboardSummaryItem,
  DashboardValidationError,
  UpdateDashboardRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type DashboardFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class DashboardApiError extends Error {
  readonly errors: DashboardValidationError[];

  constructor(message: string, errors: DashboardValidationError[] = []) {
    super(message);
    this.name = "DashboardApiError";
    this.errors = errors;
  }
}

const defaultFetcher: DashboardFetcher = (input, init) => fetch(input, init);

export async function getDashboardSummary(fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardSummary> {
  return requestJson<DashboardSummary>("/api/dashboard/summary", { method: "GET", credentials: "include" }, fetcher);
}

export async function listDashboards(fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardSummaryItem[]> {
  return requestItems<DashboardSummaryItem>("/api/dashboards", { method: "GET", credentials: "include" }, fetcher);
}

export async function getDashboard(dashboardId: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(
    `/api/dashboards/${encodeURIComponent(dashboardId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function getDashboardSharing(dashboardId: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardSharingSettings> {
  return requestJson<DashboardSharingSettings>(`/api/dashboards/${encodeURIComponent(dashboardId)}/sharing`, { method: "GET", credentials: "include" }, fetcher);
}

export async function getDashboardSharingOptions(fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardSharingOptions> {
  return requestJson<DashboardSharingOptions>("/api/dashboards/sharing-options", { method: "GET", credentials: "include" }, fetcher);
}

export async function getDashboardBySlug(slug: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(`/api/dashboards/by-slug/${encodeURIComponent(slug)}`, { method: "GET", credentials: "include" }, fetcher);
}

export async function listDashboardNavigation(fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardNavigationItem[]> {
  return requestItems<DashboardNavigationItem>("/api/dashboards/navigation", { method: "GET", credentials: "include" }, fetcher);
}

export async function publishDashboard(dashboardId: string, concurrencyStamp: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardDetail> {
  return mutatePublication(dashboardId, "publish", concurrencyStamp, fetcher);
}

export async function unpublishDashboard(dashboardId: string, concurrencyStamp: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardDetail> {
  return mutatePublication(dashboardId, "unpublish", concurrencyStamp, fetcher);
}

export async function listDashboardRevisions(dashboardId: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardRevisionSummary[]> {
  return requestItems<DashboardRevisionSummary>(`/api/dashboards/${encodeURIComponent(dashboardId)}/revisions`, { method: "GET", credentials: "include" }, fetcher);
}

export async function getDashboardPublishedComparison(dashboardId: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardPublishedComparison> {
  return requestJson<DashboardPublishedComparison>(`/api/dashboards/${encodeURIComponent(dashboardId)}/published-comparison`, { method: "GET", credentials: "include" }, fetcher);
}

export async function restoreDashboardRevision(dashboardId: string, revisionId: string, concurrencyStamp: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(`/api/dashboards/${encodeURIComponent(dashboardId)}/revisions/${encodeURIComponent(revisionId)}/restore`, {
    method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ concurrencyStamp })
  }, fetcher);
}

function mutatePublication(dashboardId: string, action: "publish" | "unpublish", concurrencyStamp: string, fetcher: DashboardFetcher): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(
    `/api/dashboards/${encodeURIComponent(dashboardId)}/${action}`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ concurrencyStamp })
    },
    fetcher
  );
}

export async function createDashboard(
  request: CreateDashboardRequest,
  fetcher: DashboardFetcher = defaultFetcher
): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(
    "/api/dashboards",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateDashboard(
  dashboardId: string,
  request: UpdateDashboardRequest,
  fetcher: DashboardFetcher = defaultFetcher
): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(
    `/api/dashboards/${encodeURIComponent(dashboardId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function deleteDashboard(dashboardId: string, concurrencyStamp: string, fetcher: DashboardFetcher = defaultFetcher): Promise<void> {
  await requestJson<null>(`/api/dashboards/${encodeURIComponent(dashboardId)}`, {
    method: "DELETE",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ concurrencyStamp })
  }, fetcher);
}

export async function listArchivedDashboards(fetcher: DashboardFetcher = defaultFetcher): Promise<ArchivedDashboard[]> {
  return requestItems<ArchivedDashboard>("/api/dashboards/archived", { method: "GET", credentials: "include" }, fetcher);
}

export async function restoreArchivedDashboard(dashboardId: string, concurrencyStamp: string, fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardDetail> {
  return requestJson<DashboardDetail>(`/api/dashboards/${encodeURIComponent(dashboardId)}/restore`, {
    method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ concurrencyStamp })
  }, fetcher);
}

export async function permanentlyDeleteDashboard(dashboardId: string, concurrencyStamp: string, confirmationName: string, fetcher: DashboardFetcher = defaultFetcher): Promise<void> {
  await requestJson<null>(`/api/dashboards/${encodeURIComponent(dashboardId)}/permanent`, {
    method: "DELETE", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ concurrencyStamp, confirmationName })
  }, fetcher);
}

export async function previewChartWidget(
  formId: string,
  request: ChartWidgetConfig,
  fetcher: DashboardFetcher = defaultFetcher
): Promise<ChartWidgetPreview> {
  return requestJson<ChartWidgetPreview>(
    `/api/forms/${encodeURIComponent(formId)}/chart-widgets/preview`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function runDashboardAnalytics(
  request: DashboardAnalyticsRequest,
  fetcher: DashboardFetcher = defaultFetcher
): Promise<DashboardAnalyticsResponse> {
  return requestJson<DashboardAnalyticsResponse>(
    "/api/dashboard/analytics/run",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: DashboardFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new DashboardApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: DashboardFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new DashboardApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
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

  return "Dashboard request failed.";
}

function getValidationErrorsFromBody(body: unknown): DashboardValidationError[] {
  if (!isRecord(body) || !Array.isArray(body.errors)) return [];

  return body.errors.filter((error): error is DashboardValidationError =>
    isRecord(error)
    && typeof error.path === "string"
    && typeof error.code === "string"
    && typeof error.message === "string"
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
