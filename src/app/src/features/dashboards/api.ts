import type { ChartWidgetConfig, ChartWidgetPreview, DashboardSummary } from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type DashboardFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class DashboardApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "DashboardApiError";
  }
}

const defaultFetcher: DashboardFetcher = (input, init) => fetch(input, init);

export async function getDashboardSummary(fetcher: DashboardFetcher = defaultFetcher): Promise<DashboardSummary> {
  return requestJson<DashboardSummary>("/api/dashboard/summary", { method: "GET", credentials: "include" }, fetcher);
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

async function requestJson<T>(input: string, init: RequestInit, fetcher: DashboardFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new DashboardApiError(getErrorMessageFromBody(body));
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

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
