import type { DashboardTemplateDefinition } from "../templateEngine";

export const businessPerformanceSampleTemplate: DashboardTemplateDefinition = {
  id: "business-performance-sample",
  version: 1,
  name: "Business Performance Sample",
  description: "A comprehensive starter dashboard for business volume, value, trends, segments, and recent records.",
  category: "Starter",
  tags: ["sample", "analytics", "business", "starter"],
  sourceSlots: [{ key: "primary", label: "Business performance data", description: "Requires amount, event_date, category, region, and priority fields.", kind: "form", required: true, allowReport: true }],
  sections: [
    { key: "executive", title: "Executive overview" },
    { key: "trends", title: "Trends" },
    { key: "segments", title: "Segments" },
    { key: "records", title: "Records" }
  ],
  filters: [
    { key: "event-date", label: "Date range", type: "date_range", sourceSlot: "primary", fieldId: "event_date" },
    { key: "status", label: "Status", type: "record_status", sourceSlot: "primary", fieldId: "status", options: ["active", "pending", "approved", "closed"] },
    { key: "category", label: "Category", type: "single_select", sourceSlot: "primary", fieldId: "category", options: ["Product", "Service", "Subscription"] },
    { key: "region", label: "Region", type: "single_select", sourceSlot: "primary", fieldId: "region", options: ["North", "South", "East", "West"] }
  ],
  widgets: [
    analytics("total-records", "Total records", "executive", "small", "number_card", "count"),
    analytics("total-amount", "Total amount", "executive", "small", "number_card", "sum", { metricFieldId: "amount" }),
    analytics("average-amount", "Average amount", "executive", "small", "number_card", "average", { metricFieldId: "amount" }),
    analytics("records-by-status", "Records by status", "executive", "medium", "choice_breakdown", "count", { groupByFieldId: "status" }),
    analytics("records-over-time", "Records over time", "trends", "wide", "date_trend", "count", { dateFieldId: "event_date" }),
    analytics("amount-over-time", "Amount over time", "trends", "wide", "date_trend", "sum", { metricFieldId: "amount", dateFieldId: "event_date" }),
    analytics("amount-by-category", "Amount by category", "segments", "wide", "choice_breakdown", "sum", { metricFieldId: "amount", groupByFieldId: "category" }),
    analytics("records-by-region", "Records by region", "segments", "wide", "choice_breakdown", "count", { groupByFieldId: "region" }),
    analytics("records-by-priority", "Records by priority", "segments", "medium", "choice_breakdown", "count", { groupByFieldId: "priority" }),
    analytics("recent-records", "Recent records", "records", "full", "table", "count", { columns: ["title", "category", "region", "priority", "amount", "status", "created_at"], limit: 20 })
  ]
};

function analytics(
  key: string,
  title: string,
  sectionKey: string,
  width: "small" | "medium" | "wide" | "full",
  widgetType: "number_card" | "choice_breakdown" | "date_trend" | "table",
  metricType: "count" | "sum" | "average",
  options: { metricFieldId?: string; groupByFieldId?: string; dateFieldId?: string; columns?: string[]; limit?: number } = {}
) {
  return {
    key, title, sectionKey, width,
    source: {
      kind: "analytics" as const,
      sourceSlot: "primary",
      chart: {
        widgetType,
        metric: { type: metricType, fieldId: options.metricFieldId ?? null },
        groupByFieldId: options.groupByFieldId ?? null,
        dateFieldId: options.dateFieldId ?? null,
        columns: options.columns ?? [],
        limit: options.limit ?? 12
      }
    }
  };
}
