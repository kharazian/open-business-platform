import type { DashboardTemplateDefinition } from "../templateEngine";

type AnalyticsOptions = {
  metricFieldId?: string;
  groupByFieldId?: string;
  dateFieldId?: string;
  columns?: string[];
  limit?: number;
  subtitle?: string;
};

export const businessPerformanceSampleTemplate: DashboardTemplateDefinition = {
  id: "business-performance-sample",
  version: 2,
  name: "Business Performance Sample",
  description: "An eleven-section reference dashboard covering business, operational, safety, target, drill-down, and data-health patterns.",
  category: "Starter",
  tags: ["sample", "analytics", "business", "operations", "finance", "hse", "starter", "showcase"],
  requiredAdapterIds: ["sample-dashboard"],
  sourceSlots: [
    { key: "business", label: "Business performance data", description: "Business volume, value, category, region, status, and record detail.", kind: "form", required: true, allowReport: true },
    { key: "operations", label: "Operational performance data", description: "Period-based Loss, Production, Engineering, Supply Chain, QAQC, target, and product facts.", kind: "form", required: true, allowReport: true },
    { key: "incidents", label: "HSE incident data", description: "Incident, injury, location, lost-time, cost, and training facts.", kind: "form", required: true, allowReport: true }
  ],
  sections: [
    { key: "executive", title: "Executive Overview", icon: "gauge" },
    { key: "financial", title: "Financial Performance", icon: "badge-dollar-sign" },
    { key: "loss", title: "Loss", icon: "trending-up" },
    { key: "production", title: "Production", icon: "factory" },
    { key: "engineering", title: "Engineering", icon: "wrench" },
    { key: "supply-chain", title: "Supply Chain", icon: "package-check" },
    { key: "qaqc", title: "QAQC", icon: "shield-check" },
    { key: "hse", title: "HSE", icon: "heart-pulse" },
    { key: "trends-targets", title: "Trends & Targets", icon: "chart-column" },
    { key: "records", title: "Records & Drill-down", icon: "clipboard-list" },
    { key: "data-health", title: "Data Health", icon: "activity" }
  ],
  filters: [
    { key: "business-date", label: "Business date", type: "date_range", sourceSlot: "business", fieldId: "event_date" },
    { key: "status", label: "Status", type: "record_status", sourceSlot: "business", fieldId: "status", options: ["active", "pending", "approved", "closed"] },
    { key: "category", label: "Category", type: "single_select", sourceSlot: "business", fieldId: "category", options: ["Product", "Service", "Subscription"] },
    { key: "region", label: "Region", type: "single_select", sourceSlot: "business", fieldId: "region", options: ["North", "South", "East", "West"] },
    { key: "fiscal-year", label: "Fiscal year", type: "single_select", sourceSlot: "operations", fieldId: "fiscal_year", options: ["2025", "2026"] },
    { key: "period-type", label: "Period", type: "single_select", sourceSlot: "operations", fieldId: "period_type", options: ["Week", "Month", "Quarter"] },
    { key: "product", label: "Product / recipe", type: "multi_select", sourceSlot: "operations", fieldId: "product", options: ["Classic", "Premium", "Light", "Specialty"] },
    { key: "location", label: "HSE location", type: "single_select", sourceSlot: "incidents", fieldId: "location", options: ["Receiving", "Processing", "Packaging", "Warehouse"] }
  ],
  widgets: [
    analytics("total-records", "Total records", "executive", "small", "business", "number_card", "count"),
    analytics("total-amount", "Total amount", "executive", "small", "business", "number_card", "sum", { metricFieldId: "amount", subtitle: "Permission-filtered business value" }),
    analytics("average-amount", "Average amount", "executive", "small", "business", "number_card", "average", { metricFieldId: "amount" }),
    analytics("records-by-status", "Records by status", "executive", "medium", "business", "choice_breakdown", "count", { groupByFieldId: "status" }),
    adapter("executive-target", "Actual versus target", "executive", "wide", "target_attainment", { actual: 92, target: 100, unit: "%", tone: "warning", sourceLabel: "Operational Performance Sample Data" }),
    analytics("amount-by-category", "Amount by category", "financial", "wide", "business", "choice_breakdown", "sum", { metricFieldId: "amount", groupByFieldId: "category" }),
    analytics("amount-over-time", "Amount over time", "financial", "wide", "business", "date_trend", "sum", { metricFieldId: "amount", dateFieldId: "event_date" }),
    adapter("finance-delta", "Net performance versus budget", "financial", "small", "kpi_delta", { actual: 207000, comparison: 198000, unit: "$", tone: "positive", sourceLabel: "Business Performance Sample Data" }),
    adapter("profitability-waterfall", "Profitability waterfall", "financial", "wide", "waterfall", { labels: "Sales|Discounts|COGS|Expenses|Net", values: "207|-12|-94|-43|58", unit: "$k", sourceLabel: "Illustrative bounded finance adapter" }),
    adapter("channel-heatmap", "Channel and product heatmap", "financial", "wide", "heatmap", { rows: "Direct|Retail|Partner", columns: "Classic|Premium|Light|Specialty", values: "82|64|58|71|61|75|68|55|49|62|78|66", sourceLabel: "Illustrative bounded finance adapter" }),
    analytics("loss-actual", "Total loss actual", "loss", "small", "operations", "number_card", "sum", { metricFieldId: "actual_value" }),
    analytics("loss-by-metric", "Loss by metric", "loss", "wide", "operations", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "metric_key" }),
    adapter("loss-target", "Loss actual and standard", "loss", "wide", "combo", { labels: "Jan|Feb|Mar|Apr|May|Jun", primary: "8|7|9|6|5|6", secondary: "7|7|7|6|6|6", unit: "%", sourceLabel: "Operational Performance Sample Data" }),
    analytics("production-by-product", "Production by product", "production", "wide", "operations", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "product" }),
    analytics("production-trend", "Production trend", "production", "wide", "operations", "date_trend", "sum", { metricFieldId: "actual_value", dateFieldId: "period_date" }),
    adapter("production-stack", "Product composition", "production", "wide", "stacked_bar", { labels: "Q1|Q2|Q3|Q4", primary: "42|48|51|55", secondary: "31|34|38|41", tertiary: "18|21|24|27", unit: "t", sourceLabel: "Operational Performance Sample Data" }),
    analytics("engineering-by-equipment", "Engineering performance", "engineering", "wide", "operations", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "equipment" }),
    analytics("engineering-trend", "Utilities and reliability trend", "engineering", "wide", "operations", "date_trend", "average", { metricFieldId: "actual_value", dateFieldId: "period_date" }),
    adapter("engineering-target", "Actual versus engineering standard", "engineering", "wide", "target_line", { labels: "W1|W2|W3|W4|W5|W6", primary: "72|69|75|71|68|66", secondary: "70|70|70|70|70|70", unit: "%", sourceLabel: "Operational Performance Sample Data" }),
    analytics("supply-by-product", "Inventory by product", "supply-chain", "wide", "operations", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "product" }),
    analytics("supply-by-metric", "Supply-chain KPI families", "supply-chain", "wide", "operations", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "metric_key" }),
    adapter("supply-attainment", "Service-level attainment", "supply-chain", "medium", "target_attainment", { actual: 96, target: 98, unit: "%", tone: "warning", sourceLabel: "Operational Performance Sample Data" }),
    analytics("qaqc-rate", "QAQC first-time release", "qaqc", "small", "operations", "number_card", "average", { metricFieldId: "actual_value" }),
    analytics("qaqc-metrics", "Quality metrics", "qaqc", "wide", "operations", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "metric_key" }),
    analytics("qaqc-detail", "Quality detail", "qaqc", "full", "operations", "table", "count", { columns: ["period_label", "metric_key", "product", "actual_value", "target_value", "unit", "status"], limit: 20 }),
    analytics("incident-count", "YTD incidents", "hse", "small", "incidents", "number_card", "count"),
    analytics("incident-cost", "Incident cost", "hse", "small", "incidents", "number_card", "sum", { metricFieldId: "incident_cost" }),
    analytics("lost-hours", "Lost hours", "hse", "small", "incidents", "number_card", "sum", { metricFieldId: "lost_hours" }),
    analytics("incidents-by-location", "Incidents by location", "hse", "wide", "incidents", "choice_breakdown", "count", { groupByFieldId: "location" }),
    analytics("injuries-over-time", "Injuries by month", "hse", "wide", "incidents", "date_trend", "count", { dateFieldId: "incident_date" }),
    adapter("incident-donut", "Incident location mix", "hse", "medium", "donut", { labels: "Receiving|Processing|Packaging|Warehouse", values: "8|12|10|6", unit: "incidents", sourceLabel: "HSE Incident Sample Data" }),
    analytics("operations-over-time", "Operational actual over time", "trends-targets", "wide", "operations", "date_trend", "sum", { metricFieldId: "actual_value", dateFieldId: "period_date" }),
    analytics("business-over-time", "Business records over time", "trends-targets", "wide", "business", "date_trend", "count", { dateFieldId: "event_date" }),
    adapter("actual-budget", "Actual and budget comparison", "trends-targets", "wide", "combo", { labels: "Jan|Feb|Mar|Apr|May|Jun", primary: "31|35|39|42|46|49", secondary: "30|34|38|43|45|48", unit: "$k", sourceLabel: "Business Performance Sample Data" }),
    adapter("period-diagnostic", "Actual-through period coverage", "trends-targets", "medium", "status_panel", { status: "warning", title: "Actual through June", detail: "Six future target-only periods remain visible.", count: 12, sourceLabel: "Operational Performance Sample Data" }),
    analytics("recent-business", "Recent business records", "records", "full", "business", "table", "count", { columns: ["title", "category", "region", "priority", "amount", "status", "created_at"], limit: 20 }),
    analytics("recent-operations", "Operational detail", "records", "full", "operations", "table", "count", { columns: ["module", "metric_key", "period_label", "product", "actual_value", "target_value", "unit"], limit: 20 }),
    adapter("detail-popup", "Period detail preview", "records", "wide", "detail_popup", { title: "Selected period detail", period: "2026 Q2", rows: 18, groups: "Product|Line|Metric", sourceLabel: "Operational Performance Sample Data" }),
    adapter("source-health", "Source health", "data-health", "wide", "data_health", { businessRows: 48, operationsRows: 72, incidentRows: 36, issues: 0, updated: "2026-08-21", sourceLabel: "Development sample sources" }),
    adapter("schema-health", "Schema and permissions", "data-health", "wide", "status_panel", { status: "success", title: "All required fields available", detail: "Three permitted sources validated; no hidden fields requested.", count: 3, sourceLabel: "Dashboard validation" })
  ]
};

function analytics(
  key: string,
  title: string,
  sectionKey: string,
  width: "small" | "medium" | "wide" | "full",
  sourceSlot: "business" | "operations" | "incidents",
  widgetType: "number_card" | "choice_breakdown" | "date_trend" | "table",
  metricType: "count" | "sum" | "average",
  options: AnalyticsOptions = {}
) {
  return {
    key, title, subtitle: options.subtitle, sectionKey, width,
    source: {
      kind: "analytics" as const,
      sourceSlot,
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

function adapter(
  key: string,
  title: string,
  sectionKey: string,
  width: "small" | "medium" | "wide" | "full",
  visualizationId: string,
  settings: Record<string, string | number | boolean | null>
) {
  return {
    key, title, sectionKey, width,
    source: { kind: "adapter" as const, adapter: { adapterId: "sample-dashboard", visualizationId, settings } }
  };
}
