import type {
  ChartMetricType,
  ChartWidgetConfig,
  ChartWidgetType,
  DashboardAnalyticsRequest,
  DashboardAnalyticsWidgetType
} from "./types";
import type { EntityId } from "../../types/entities";

export type DashboardAnalyticsBuilderConfig = {
  widgetType: DashboardAnalyticsWidgetType;
  metricType: ChartMetricType;
  metricFieldId?: string | null;
  groupByFieldId?: string | null;
  dateFieldId?: string | null;
  columns?: string[] | null;
  limit?: number | null;
  reportId?: EntityId | null;
};

export function buildChartConfigFromDashboardAnalytics(config: DashboardAnalyticsBuilderConfig): ChartWidgetConfig {
  return {
    widgetType: toChartWidgetType(config.widgetType),
    metric: {
      type: config.metricType,
      fieldId: config.metricType === "count" ? null : normalizeOptional(config.metricFieldId)
    },
    groupByFieldId: config.widgetType === "breakdown" ? normalizeOptional(config.groupByFieldId) : null,
    dateFieldId: config.widgetType === "trend" ? normalizeOptional(config.dateFieldId) : null,
    columns: config.widgetType === "table" ? normalizeColumns(config.columns) : [],
    limit: config.limit ?? 10,
    reportId: config.reportId || null
  };
}

export function buildDashboardAnalyticsRequest(formId: EntityId, chart: ChartWidgetConfig): DashboardAnalyticsRequest {
  return {
    widgetType: toDashboardAnalyticsWidgetType(chart.widgetType),
    source: {
      formId,
      reportId: chart.reportId ?? null
    },
    metric: chart.metric,
    groupByFieldId: chart.groupByFieldId ?? null,
    dateFieldId: chart.dateFieldId ?? null,
    columns: normalizeColumns(chart.columns),
    limit: chart.limit ?? 10
  };
}

export function hasRequiredDashboardAnalyticsConfig(config: DashboardAnalyticsBuilderConfig): boolean {
  if (config.metricType !== "count" && !normalizeOptional(config.metricFieldId)) {
    return false;
  }

  if (config.widgetType === "breakdown" && !normalizeOptional(config.groupByFieldId)) {
    return false;
  }

  if (config.widgetType === "trend" && !normalizeOptional(config.dateFieldId)) {
    return false;
  }

  if (config.widgetType === "table" && normalizeColumns(config.columns).length === 0) {
    return false;
  }

  return true;
}

export function toChartWidgetType(widgetType: DashboardAnalyticsWidgetType): ChartWidgetType {
  switch (widgetType) {
    case "summary":
      return "number_card";
    case "breakdown":
      return "choice_breakdown";
    case "trend":
      return "date_trend";
    case "table":
      return "table";
  }
}

export function toDashboardAnalyticsWidgetType(widgetType: ChartWidgetType): DashboardAnalyticsWidgetType {
  switch (widgetType) {
    case "number_card":
      return "summary";
    case "bar_chart":
    case "choice_breakdown":
      return "breakdown";
    case "date_trend":
      return "trend";
    case "table":
      return "table";
  }
}

function normalizeOptional(value?: string | null): string | null {
  const normalized = value?.trim();
  return normalized ? normalized : null;
}

function normalizeColumns(columns?: string[] | null): string[] {
  return (columns ?? [])
    .map((column) => column.trim())
    .filter((column, index, values) => column.length > 0 && values.indexOf(column) === index);
}
