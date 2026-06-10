import type { IntegrationLogDto, IntegrationLogFilters } from "./types";

export function isIntegrationLogRetryEligible(log: Pick<IntegrationLogDto, "status" | "isRetryable" | "retryRequestedAt">): boolean {
  return log.status === "failed" && log.isRetryable && !log.retryRequestedAt;
}

export function filterIntegrationLogs(logs: IntegrationLogDto[], filters: IntegrationLogFilters): IntegrationLogDto[] {
  const source = filters.source?.trim().toLowerCase() ?? "";
  const since = filters.since ? Date.parse(filters.since) : Number.NaN;

  return logs.filter((log) => {
    if (filters.direction && log.direction !== filters.direction) return false;
    if (filters.status && log.status !== filters.status) return false;
    if (filters.type && log.integrationType !== filters.type) return false;
    if (source && !`${log.sourceType} ${log.integrationKey}`.toLowerCase().includes(source)) return false;
    if (!Number.isNaN(since) && Date.parse(log.startedAt) < since) return false;
    return true;
  });
}

export function formatIntegrationDate(value?: string | null): string {
  if (!value) return "-";
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

export function formatMetadata(value: unknown): string {
  if (value == null) return "No metadata";
  return JSON.stringify(value, null, 2);
}
