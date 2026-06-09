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

export const dashboardAnalyticsWidgetTypes = ["summary", "breakdown", "trend", "table"] as const;
export const dashboardAnalyticsMetricTypes = ["count", "sum", "average"] as const;

export type DashboardAnalyticsWidgetType = (typeof dashboardAnalyticsWidgetTypes)[number];
export type DashboardAnalyticsMetricType = (typeof dashboardAnalyticsMetricTypes)[number];

export type DashboardAnalyticsSource = {
  formId: EntityId;
  reportId?: EntityId | null;
};

export type DashboardAnalyticsMetric = {
  type: DashboardAnalyticsMetricType;
  fieldId?: string | null;
};

export type DashboardAnalyticsRequest = {
  widgetType: DashboardAnalyticsWidgetType;
  source: DashboardAnalyticsSource;
  metric: DashboardAnalyticsMetric;
  groupByFieldId?: string | null;
  dateFieldId?: string | null;
  columns?: string[] | null;
  limit?: number | null;
};

export type DashboardAnalyticsResponse = {
  formId: EntityId;
  formName: string;
  reportId?: EntityId | null;
  widgetType: DashboardAnalyticsWidgetType;
  metric: DashboardAnalyticsMetric;
  series: ChartSeriesPoint[];
  columns: ChartTableColumn[];
  rows: ChartTableRow[];
  totalCount: number;
};

export const dashboardWidgetWidths = ["small", "medium", "wide", "full"] as const;
export const dashboardVisibilityModes = ["workspace", "private"] as const;

export type DashboardWidgetWidth = (typeof dashboardWidgetWidths)[number];
export type DashboardVisibility = (typeof dashboardVisibilityModes)[number];

export type DashboardSettings = {
  visibility: DashboardVisibility;
  isDefault: boolean;
};

export type SavedDashboardWidget = {
  id: string;
  title: string;
  sourceFormId: EntityId;
  chart: ChartWidgetConfig;
};

export type SavedDashboardConfig = {
  schemaVersion: 1;
  widgets: SavedDashboardWidget[];
};

export type SavedDashboardWidgetLayout = {
  id: string;
  width: DashboardWidgetWidth;
  order: number;
};

export type SavedDashboardLayout = {
  schemaVersion: 1;
  widgets: SavedDashboardWidgetLayout[];
};

export type DashboardSummaryItem = {
  id: EntityId;
  name: string;
  description?: string | null;
  widgetCount: number;
  visibility: DashboardVisibility;
  isDefault: boolean;
  concurrencyStamp: string;
  createdAt: string;
  createdById?: EntityId | null;
  updatedAt?: string | null;
  updatedById?: EntityId | null;
};

export type DashboardDetail = DashboardSummaryItem & {
  config: SavedDashboardConfig;
  layout: SavedDashboardLayout;
};

export type CreateDashboardRequest = {
  name: string;
  description?: string | null;
  config: SavedDashboardConfig;
  layout: SavedDashboardLayout;
  settings?: DashboardSettings;
};

export type UpdateDashboardRequest = CreateDashboardRequest & {
  concurrencyStamp: string;
};
