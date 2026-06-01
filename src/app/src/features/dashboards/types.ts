import type { EntityId } from "../../types/entities";

export type DashboardMetric = {
  key: string;
  label: string;
  value: number;
};

export type DashboardActivityItem = {
  id: EntityId;
  event: string;
  actor: string;
  createdAt: string;
  status: string;
};

export type DashboardSummary = {
  title: string;
  metrics: DashboardMetric[];
  recentActivity: DashboardActivityItem[];
};

export const chartWidgetTypes = ["number_card", "bar_chart", "date_trend", "choice_breakdown", "table"] as const;
export const chartMetricTypes = ["count", "sum", "average"] as const;

export type ChartWidgetType = (typeof chartWidgetTypes)[number];
export type ChartMetricType = (typeof chartMetricTypes)[number];

export type ChartMetricDefinition = {
  type: ChartMetricType;
  fieldId?: string | null;
};

export type ChartWidgetConfig = {
  widgetType: ChartWidgetType;
  metric: ChartMetricDefinition;
  groupByFieldId?: string | null;
  dateFieldId?: string | null;
  columns?: string[] | null;
  limit?: number | null;
  reportId?: EntityId | null;
};

export type ChartSeriesPoint = {
  key: string;
  label: string;
  value: number;
};

export type ChartTableColumn = {
  fieldId: string;
  label: string;
  type: string;
  source: "form" | "system";
};

export type ChartTableCell = {
  value: string | number | boolean | null;
  displayValue: string;
};

export type ChartTableRow = {
  recordId: EntityId;
  status: string;
  cells: Record<string, ChartTableCell | undefined>;
  createdAt: string;
};

export type ChartWidgetPreview = {
  formId: EntityId;
  formName: string;
  widgetType: ChartWidgetType;
  metric: ChartMetricDefinition;
  columns: ChartTableColumn[];
  series: ChartSeriesPoint[];
  rows: ChartTableRow[];
  totalCount: number;
};
