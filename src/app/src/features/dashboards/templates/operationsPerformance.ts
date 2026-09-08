import type { DashboardTemplateDefinition } from "../templateEngine";

type AnalyticsOptions = {
  metricFieldId?: string;
  groupByFieldId?: string;
  dateFieldId?: string;
  dateGranularity?: "day" | "week" | "month" | "quarter" | "year";
  columns?: string[];
  limit?: number;
  subtitle?: string;
  fixedModule?: "Loss" | "Production" | "Engineering" | "Supply Chain" | "QAQC";
};

export const operationsPerformanceTemplate: DashboardTemplateDefinition = {
  id: "operations-performance",
  version: 2,
  name: "Operations Performance",
  description: "A focused operations dashboard covering overview, loss, production, engineering, supply chain, QA/QC, trends, and record detail.",
  category: "Operations",
  tags: ["operations", "loss", "production", "engineering", "supply-chain", "qaqc", "starter"],
  requiredAdapterIds: ["sample-dashboard"],
  sourceSlots: [{
    key: "operations", label: "Operational performance data",
    description: "Period-based operational facts for loss, production, engineering, supply chain, QA/QC, targets, products, and equipment.",
    kind: "form", required: true, allowReport: true
  }],
  sections: [
    { key: "overview", title: "Operations Overview", icon: "gauge" },
    { key: "loss", title: "Loss", icon: "trending-up" },
    { key: "production", title: "Production", icon: "factory" },
    { key: "engineering", title: "Engineering", icon: "wrench" },
    { key: "supply-chain", title: "Supply Chain", icon: "package-check" },
    { key: "qaqc", title: "QA/QC", icon: "shield-check" },
    { key: "trends-records", title: "Trends & Records", icon: "clipboard-list" }
  ],
  filters: [
    { key: "fiscal-year", label: "Fiscal year", type: "single_select", sourceSlot: "operations", fieldId: "fiscal_year", options: ["2025", "2026"] },
    { key: "period-type", label: "Period", type: "single_select", sourceSlot: "operations", fieldId: "period_type", options: ["Week", "Month", "Quarter"] },
    { key: "product", label: "Product / recipe", type: "multi_select", sourceSlot: "operations", fieldId: "product", options: ["Classic", "Premium", "Light", "Specialty"], applyToWidgetKeys: ["production-by-product", "production-trend", "supply-by-product", "supply-by-metric", "qaqc-rate", "qaqc-metrics", "qaqc-detail", "recent-operations"] },
    { key: "equipment", label: "Equipment", type: "multi_select", sourceSlot: "operations", fieldId: "equipment", options: ["Line 1", "Line 2", "Dryer", "Packaging"], applyToWidgetKeys: ["engineering-by-equipment", "engineering-trend", "recent-operations"] },
    { key: "module", label: "Module", type: "single_select", sourceSlot: "operations", fieldId: "module", options: ["Loss", "Production", "Engineering", "Supply Chain", "QAQC"], applyToWidgetKeys: ["operational-facts", "total-actual", "total-target", "performance-by-module", "operations-over-time", "recent-operations"] }
  ],
  widgets: [
    analytics("operational-facts", "Operational facts", "overview", "small", "number_card", "count"),
    analytics("total-actual", "Total actual", "overview", "small", "number_card", "sum", { metricFieldId: "actual_value" }),
    analytics("total-target", "Total target", "overview", "small", "number_card", "sum", { metricFieldId: "target_value" }),
    analytics("performance-by-module", "Performance by module", "overview", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "module" }),
    adapter("overview-target", "Actual versus target", "overview", "wide", "target_attainment", { actual: 92, target: 100, unit: "%", tone: "warning", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("loss-actual", "Total loss actual", "loss", "small", "number_card", "sum", { metricFieldId: "actual_value", fixedModule: "Loss" }),
    analytics("loss-by-metric", "Loss by metric", "loss", "wide", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "metric_key", fixedModule: "Loss" }),
    adapter("loss-target", "Loss actual and standard", "loss", "wide", "combo", { labels: "Jan|Feb|Mar|Apr|May|Jun", primary: "8|7|9|6|5|6", secondary: "7|7|7|6|6|6", unit: "%", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("production-by-product", "Production by product", "production", "wide", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "product", fixedModule: "Production" }),
    analytics("production-trend", "Production trend", "production", "wide", "date_trend", "sum", { metricFieldId: "actual_value", dateFieldId: "period_date", dateGranularity: "month", fixedModule: "Production" }),
    adapter("production-stack", "Product composition", "production", "wide", "stacked_bar", { labels: "Q1|Q2|Q3|Q4", primary: "42|48|51|55", secondary: "31|34|38|41", tertiary: "18|21|24|27", unit: "t", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("engineering-by-equipment", "Engineering performance", "engineering", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "equipment", fixedModule: "Engineering" }),
    analytics("engineering-trend", "Utilities and reliability trend", "engineering", "wide", "date_trend", "average", { metricFieldId: "actual_value", dateFieldId: "period_date", dateGranularity: "month", fixedModule: "Engineering" }),
    adapter("engineering-target", "Actual versus engineering standard", "engineering", "wide", "target_line", { labels: "W1|W2|W3|W4|W5|W6", primary: "72|69|75|71|68|66", secondary: "70|70|70|70|70|70", unit: "%", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("supply-by-product", "Inventory by product", "supply-chain", "wide", "choice_breakdown", "sum", { metricFieldId: "actual_value", groupByFieldId: "product", fixedModule: "Supply Chain" }),
    analytics("supply-by-metric", "Supply-chain KPI families", "supply-chain", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "metric_key", fixedModule: "Supply Chain" }),
    adapter("supply-attainment", "Service-level attainment", "supply-chain", "medium", "target_attainment", { actual: 96, target: 98, unit: "%", tone: "warning", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("qaqc-rate", "QA/QC first-time release", "qaqc", "small", "number_card", "average", { metricFieldId: "actual_value", fixedModule: "QAQC" }),
    analytics("qaqc-metrics", "Quality metrics", "qaqc", "wide", "choice_breakdown", "average", { metricFieldId: "actual_value", groupByFieldId: "metric_key", fixedModule: "QAQC" }),
    analytics("qaqc-detail", "Quality detail", "qaqc", "full", "table", "count", { columns: ["period_label", "metric_key", "product", "actual_value", "target_value", "unit", "status"], limit: 20, fixedModule: "QAQC" }),
    analytics("operations-over-time", "Operational actual over time", "trends-records", "wide", "date_trend", "sum", { metricFieldId: "actual_value", dateFieldId: "period_date", dateGranularity: "month" }),
    adapter("actual-budget", "Actual and budget comparison", "trends-records", "wide", "combo", { labels: "Jan|Feb|Mar|Apr|May|Jun", primary: "31|35|39|42|46|49", secondary: "30|34|38|43|45|48", unit: "%", sourceLabel: "Illustrative Operations sample adapter" }),
    analytics("recent-operations", "Operational detail", "trends-records", "full", "table", "count", { columns: ["module", "metric_key", "period_label", "period_number", "product", "equipment", "actual_value", "target_value", "budget_value", "numerator", "denominator", "unit"], limit: 20 }),
    adapter("detail-popup", "Period detail preview", "trends-records", "wide", "detail_popup", { title: "Selected period detail", period: "2026 Q2", rows: 18, groups: "Module|Product|Equipment|Metric", sourceLabel: "Illustrative Operations sample adapter" })
  ]
};

function analytics(
  key: string,
  title: string,
  sectionKey: string,
  width: "small" | "medium" | "wide" | "full",
  widgetType: "number_card" | "choice_breakdown" | "date_trend" | "table",
  metricType: "count" | "sum" | "average",
  options: AnalyticsOptions = {}
) {
  return {
    key, title, subtitle: options.subtitle, sectionKey, width,
    source: {
      kind: "analytics" as const,
      sourceSlot: "operations",
      chart: {
        widgetType,
        metric: { type: metricType, fieldId: options.metricFieldId ?? null },
        groupByFieldId: options.groupByFieldId ?? null,
        dateFieldId: options.dateFieldId ?? null,
        dateGranularity: options.dateGranularity ?? "day",
        columns: options.columns ?? [],
        limit: options.limit ?? 12,
        fixedFilters: options.fixedModule ? [{ fieldId: "module", values: [options.fixedModule] }] : []
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
