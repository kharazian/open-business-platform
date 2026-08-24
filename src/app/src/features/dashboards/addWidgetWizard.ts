import type { ReportableField } from "../forms/reportableFields";
import type { DashboardAnalyticsWidgetType } from "./types";

export const dashboardVisualizationCatalog: Array<{ type: DashboardAnalyticsWidgetType; name: string; description: string; searchTerms: string }> = [
  { type: "summary", name: "KPI / summary", description: "Highlight one important total or average.", searchTerms: "kpi number card total metric" },
  { type: "breakdown", name: "Category breakdown", description: "Compare values across status or choice groups.", searchTerms: "bar category group choice status" },
  { type: "trend", name: "Time trend", description: "Track a metric across dates or periods.", searchTerms: "line area time date trend" },
  { type: "table", name: "Record table", description: "Show permitted record fields in a compact table.", searchTerms: "rows details records columns table" }
];

export function getVisualizationAvailability(fields: ReportableField[]): Record<DashboardAnalyticsWidgetType, { available: boolean; recommendation: string }> {
  const hasGroups = fields.some((field) => field.supportsChoiceGrouping);
  const hasDates = fields.some((field) => field.type === "date" || field.type === "datetime");
  return {
    summary: { available: fields.length > 0, recommendation: "Recommended for a fast headline metric." },
    breakdown: { available: hasGroups, recommendation: hasGroups ? "Recommended: this source has groupable fields." : "Requires a status or choice field." },
    trend: { available: hasDates, recommendation: hasDates ? "Recommended: this source has date fields." : "Requires a date field." },
    table: { available: fields.length > 0, recommendation: "Recommended for record-level detail." }
  };
}

export function filterDashboardVisualizations(query: string) {
  const normalized = query.trim().toLowerCase();
  return normalized ? dashboardVisualizationCatalog.filter((item) => `${item.name} ${item.description} ${item.searchTerms}`.toLowerCase().includes(normalized)) : dashboardVisualizationCatalog;
}

const recentStorageKey = "obp.dashboard.recent-visualizations.v1";

export function readRecentDashboardVisualizations(storage: Pick<Storage, "getItem"> | null): DashboardAnalyticsWidgetType[] {
  if (!storage) return [];
  try {
    const parsed = JSON.parse(storage.getItem(recentStorageKey) ?? "[]");
    return Array.isArray(parsed) ? parsed.filter((value): value is DashboardAnalyticsWidgetType => dashboardVisualizationCatalog.some((item) => item.type === value)).slice(0, 3) : [];
  } catch { return []; }
}

export function saveRecentDashboardVisualization(storage: Pick<Storage, "setItem"> | null, type: DashboardAnalyticsWidgetType, current: DashboardAnalyticsWidgetType[]) {
  const next = [type, ...current.filter((item) => item !== type)].slice(0, 3);
  try { storage?.setItem(recentStorageKey, JSON.stringify(next)); } catch { /* Recent choices are non-authoritative. */ }
  return next;
}
