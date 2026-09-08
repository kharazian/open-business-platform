import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";
import {
  buildChartConfigFromDashboardAnalytics,
  buildDashboardAnalyticsRequest,
  createDashboardPreviewStates,
  getDashboardAnalyticsWidgetLabel,
  getDashboardVisibilityLabel,
  normalizeDashboardSettings,
  hasRequiredDashboardAnalyticsConfig,
  toggleDashboardFixedFilterValue
} from "./analytics.ts";
import { getDashboardWidgetGridClass, moveDashboardLayoutWidget, orderDashboardLayoutWidgets } from "./layout.ts";
import { cloneDashboardWidgetForEditing, isDashboardAnalyticsWidgetDraftValid } from "./components/DashboardWidgetPropertiesDrawer.tsx";
import { shouldRenderConfiguredSeriesChart } from "./components/ChartWidgetPreview.tsx";
import { getDashboardAxisMaximum, getDashboardCircularSegments, getDashboardDataLabelText, getDashboardOrderedCategoryKeys, getDashboardPresentedSeries, getDashboardStackedBarSegment, hasDashboardNegativeSeriesValues, isDashboardAxisClipped, isDashboardCircularDisplayType, isDashboardSeriesPresentationValid, resolveDashboardAxisMaximum } from "./chartPresentation.ts";
import { cloneDashboardChartAppearance, defaultDashboardChartAppearance, formatDashboardCount, formatDashboardValue, getDashboardAccentColor, getDashboardConditionalResult, getDashboardEffectiveCardAccent, getDashboardKpiTargetSummary, getDashboardSeriesColor, getDashboardTooltipText, isDashboardBarModeValid, isDashboardBarOrientationValid, isDashboardCategoryLimitValid, isDashboardCategorySortValid, isDashboardChartAxesValid, isDashboardConditionalFormattingValid, isDashboardDataLabelSettingsValid, isDashboardKpiTargetValid, isDashboardLegendPositionValid, isDashboardReferenceLinesValid, isDashboardTooltipContentValid, isDashboardValueAffixValid, normalizeDashboardCategoryLimit, normalizeDashboardChartAxes, normalizeDashboardDataLabelSettings, resolveDashboardChartAppearance } from "./appearance.ts";
import { filterDashboardVisualizations, getVisualizationAvailability, readRecentDashboardVisualizations, saveRecentDashboardVisualization } from "./addWidgetWizard.ts";
import { appendBoundedCanvasHistory, canDuplicateDashboardSection, dashboardCanvasQualityLimits, getAdjacentDashboardSectionId, moveDashboardWidgetWithinSection, runDashboardTasksWithConcurrency, toggleDashboardWidgetSelection } from "./canvasProductivity.ts";
import { readDashboardViewerUrlState, writeDashboardViewerUrlState } from "./viewerState.ts";

test("dashboard viewer URL state round-trips bounded permitted filters", () => {
  const definitions = [
    { id: "region", label: "Region", type: "multi_select", sourceFormId: "form-1", fieldId: "region", options: ["North", "South"] },
    { id: "period", label: "Period", type: "date_range", sourceFormId: "form-1", fieldId: "event_date" }
  ];
  const written = writeDashboardViewerUrlState("overview", definitions, {
    region: { fieldId: "region", values: ["North", "Unknown", "South"] },
    period: { fieldId: "event_date", start: "2026-01-01", end: "2026-04-01" }
  });
  const parsed = readDashboardViewerUrlState(written, new Set(["overview"]), definitions);
  assert.equal(parsed.activeSectionId, "overview");
  assert.deepEqual(parsed.filters.region.values, ["North", "South"]);
  assert.deepEqual(parsed.filters.period, { fieldId: "event_date", start: "2026-01-01", end: "2026-04-01" });
  const rejected = readDashboardViewerUrlState(new URLSearchParams("dv=1&tab=missing&filter.region=Unknown&filter.period.start=not-a-date"), new Set(["overview"]), definitions);
  assert.deepEqual(rejected, { activeSectionId: null, filters: {} });
});

test("dashboard API client maps saved dashboard requests and errors", async () => {
  const calls = [];
  const request = {
    name: "Team dashboard",
    description: "Saved widgets",
    config: {
      schemaVersion: 1,
      widgets: [
        {
          id: "widget-1",
          title: "Employees by department",
          sourceFormId: "form-1",
          chart: {
            widgetType: "bar_chart",
            metric: { type: "count", fieldId: null },
            groupByFieldId: "department",
            dateFieldId: null,
            columns: [],
            limit: 10,
            reportId: null
          }
        }
      ]
    },
    layout: {
      schemaVersion: 1,
      widgets: [{ id: "widget-1", width: "wide", order: 1 }]
    },
    settings: {
      visibility: "workspace",
      isDefault: true
    }
  };
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/dashboards" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [
            {
              id: "dash-1",
              name: "Team dashboard",
              widgetCount: 1,
              visibility: "workspace",
              isDefault: true,
              concurrencyStamp: "stamp-1",
              createdAt: "2026-06-01T12:00:00.000Z"
            }
          ]
        })
      };
    }

    if (input === "/api/dashboards/dash-1" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          id: "dash-1",
          concurrencyStamp: "stamp-1",
          createdAt: "2026-06-01T12:00:00.000Z",
          ...request,
          visibility: "workspace",
          isDefault: true
        })
      };
    }

    if (input === "/api/dashboards" && init.method === "POST") {
      return {
        ok: true,
        json: async () => ({
          id: "dash-1",
          concurrencyStamp: "stamp-1",
          createdAt: "2026-06-01T12:00:00.000Z",
          ...request,
          visibility: "workspace",
          isDefault: true
        })
      };
    }

    if (input === "/api/dashboards/dash-1" && init.method === "PUT") {
      return {
        ok: true,
        json: async () => ({
          id: "dash-1",
          concurrencyStamp: "stamp-2",
          createdAt: "2026-06-01T12:00:00.000Z",
          ...request,
          visibility: "workspace",
          isDefault: true
        })
      };
    }

    if (input === "/api/dashboards/dash-1" && init.method === "DELETE") {
      return { ok: true, json: async () => null };
    }

    if (input === "/api/dashboards/archived" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [{
            id: "dash-1",
            name: "Team dashboard",
            widgetCount: 1,
            archivedAt: "2026-06-02T12:00:00.000Z",
            concurrencyStamp: "stamp-3",
            permanentDeleteAvailableAt: "2026-07-02T12:00:00.000Z",
            canDeletePermanently: false
          }]
        })
      };
    }

    if (input === "/api/dashboards/dash-1/restore" && init.method === "POST") {
      return { ok: true, json: async () => ({ id: "dash-1", name: "Team dashboard", concurrencyStamp: "stamp-4", ...request }) };
    }

    if (input === "/api/dashboards/dash-1/permanent" && init.method === "DELETE") {
      return { ok: true, json: async () => null };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const summaries = await api.listDashboards(fetcher);
  const detail = await api.getDashboard("dash-1", fetcher);
  const created = await api.createDashboard(request, fetcher);
  const updated = await api.updateDashboard("dash-1", { ...request, concurrencyStamp: "stamp-1" }, fetcher);
  await api.deleteDashboard("dash-1", "stamp-2", fetcher);
  const archived = await api.listArchivedDashboards(fetcher);
  const restored = await api.restoreArchivedDashboard("dash-1", "stamp-3", fetcher);
  await api.permanentlyDeleteDashboard("dash-1", "stamp-4", "Team dashboard", fetcher);

  assert.equal(summaries[0].widgetCount, 1);
  assert.equal(summaries[0].visibility, "workspace");
  assert.equal(summaries[0].isDefault, true);
  assert.equal(detail.config.widgets[0].title, "Employees by department");
  assert.equal(detail.visibility, "workspace");
  assert.equal(detail.isDefault, true);
  assert.equal(created.id, "dash-1");
  assert.equal(updated.concurrencyStamp, "stamp-2");
  assert.equal(archived[0].canDeletePermanently, false);
  assert.equal(restored.concurrencyStamp, "stamp-4");
  assert.equal(calls[0].input, "/api/dashboards");
  assert.equal(calls[2].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[2].init.body), request);
  const archiveCall = calls.find((call) => call.input === "/api/dashboards/dash-1" && call.init.method === "DELETE");
  assert.deepEqual(JSON.parse(archiveCall.init.body), { concurrencyStamp: "stamp-2" });
  assert.deepEqual(JSON.parse(calls.at(-1).init.body), { concurrencyStamp: "stamp-4", confirmationName: "Team dashboard" });

  await assert.rejects(
    () => api.listDashboards(async () => ({ ok: false, json: async () => ({ message: "Dashboard access denied." }) })),
    (error) => {
      assert.equal(error.name, "DashboardApiError");
      assert.equal(error.message, "Dashboard access denied.");
      return true;
    }
  );
});

test("dashboard layout helpers sort widgets and map widths", () => {
  const ordered = orderDashboardLayoutWidgets([
    { id: "b", width: "small", order: 2 },
    { id: "a", width: "full", order: 1 }
  ]);

  assert.deepEqual(ordered.map((widget) => widget.id), ["a", "b"]);
  assert.equal(getDashboardWidgetGridClass("small"), "md:col-span-3");
  assert.equal(getDashboardWidgetGridClass("medium"), "md:col-span-6");
  assert.equal(getDashboardWidgetGridClass("wide"), "md:col-span-9");
  assert.equal(getDashboardWidgetGridClass("full"), "md:col-span-12");
  assert.deepEqual(moveDashboardLayoutWidget(ordered, "b", "a").map((widget) => widget.id), ["b", "a"]);
  assert.deepEqual(moveDashboardLayoutWidget(ordered, "a", null).map((widget) => widget.id), ["b", "a"]);
});

test("widget property drafts clone nested config and validate permitted fields", () => {
  const widget = { id: "widget-1", title: "Amount", sourceFormId: "form-1", sectionId: "overview", chart: { widgetType: "choice_breakdown", metric: { type: "sum", fieldId: "amount" }, groupByFieldId: "status", columns: [], limit: 10, series: [{ id: "amount", label: "Amount", metric: { type: "sum", fieldId: "amount" }, displayType: "bar", color: "primary", axis: "left" }], appearance: { ...defaultDashboardChartAppearance, palette: "warm", cardAccent: "warning", conditionalFormatting: { enabled: false, rules: [{ id: "target", operator: "greater_or_equal", value: 100, accent: "success" }] } }, fixedFilters: [{ fieldId: "status", values: ["active"] }] } };
  const draft = cloneDashboardWidgetForEditing(widget);
  draft.chart.metric.fieldId = "other";
  draft.chart.series[0].metric.fieldId = "other";
  draft.chart.appearance.palette = "mono";
  draft.chart.appearance.conditionalFormatting.rules[0].value = 120;
  draft.chart.fixedFilters[0].values[0] = "closed";
  assert.equal(widget.chart.metric.fieldId, "amount");
  assert.equal(widget.chart.series[0].metric.fieldId, "amount");
  assert.equal(widget.chart.appearance.palette, "warm");
  assert.equal(widget.chart.appearance.conditionalFormatting.rules[0].value, 100);
  assert.equal(widget.chart.fixedFilters[0].values[0], "active");
  const fields = [
    { id: "amount", label: "Amount", type: "currency", source: "form", options: [], filterable: true, sortable: true, searchable: false, supportsAggregation: true, supportsChoiceGrouping: false },
    { id: "status", label: "Status", type: "status", source: "system", options: [{ id: "active", label: "Active", value: "active" }, { id: "closed", label: "Closed", value: "closed" }], filterable: true, sortable: true, searchable: true, supportsAggregation: false, supportsChoiceGrouping: true },
    { id: "event_date", label: "Event date", type: "date", source: "form", options: [], filterable: true, sortable: true, searchable: false, supportsAggregation: false, supportsChoiceGrouping: false },
    { id: "restricted", label: "Restricted", type: "text", source: "form", options: [], filterable: false, sortable: false, searchable: false, supportsAggregation: false, supportsChoiceGrouping: false }
  ];
  assert.equal(isDashboardAnalyticsWidgetDraftValid(widget, fields), true);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "hidden", values: ["x"] }] } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "restricted", values: ["x"] }] } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "status", values: ["unknown"] }] } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "event_date", start: "2026-01-01", end: "2026-02-01" }] } }, fields), true);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "event_date", start: "2026-02-01", end: "2026-01-01" }] } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "event_date", values: ["2026-01-01"] }] } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, fixedFilters: [{ fieldId: "status", values: ["active"], start: "2026-01-01" }] } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, groupByFieldId: "hidden" } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, dateGranularity: "fortnight" } }, fields), false);
  const comparisonWidget = { ...widget, chart: { ...widget.chart, widgetType: "number_card", groupByFieldId: null, kpiComparison: { enabled: true, dateFieldId: "event_date", period: "last_30_days" } } };
  assert.equal(isDashboardAnalyticsWidgetDraftValid(comparisonWidget, fields), true);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...comparisonWidget, chart: { ...comparisonWidget.chart, kpiComparison: { enabled: true, dateFieldId: "status", period: "last_30_days" } } }, fields), false);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...comparisonWidget, chart: { ...comparisonWidget.chart, kpiComparison: { enabled: true, dateFieldId: "event_date", period: "unsupported" } } }, fields), false);
  assert.deepEqual(buildDashboardAnalyticsRequest("form-1", comparisonWidget.chart).kpiComparison, { enabled: true, dateFieldId: "event_date", period: "last_30_days" });
  const comparisonClone = cloneDashboardWidgetForEditing(comparisonWidget);
  comparisonClone.chart.kpiComparison.period = "last_7_days";
  assert.equal(comparisonWidget.chart.kpiComparison.period, "last_30_days");
});

test("fixed choice filters toggle several values without mutating the saved filter", () => {
  const saved = { fieldId: "module", values: ["Loss"] };
  const added = toggleDashboardFixedFilterValue(saved, "Production", true);
  assert.deepEqual(added, { fieldId: "module", values: ["Loss", "Production"] });
  assert.deepEqual(saved, { fieldId: "module", values: ["Loss"] });
  assert.deepEqual(toggleDashboardFixedFilterValue(added, "Loss", false), { fieldId: "module", values: ["Production"] });
  assert.deepEqual(toggleDashboardFixedFilterValue(added, "Production", true), added);
});

test("dashboard appearance helpers preserve defaults, palettes, accents, and localized formats", () => {
  const defaults = resolveDashboardChartAppearance(null);
  assert.deepEqual(defaults, defaultDashboardChartAppearance);
  assert.equal(getDashboardSeriesColor("primary", "cool"), "#2563eb");
  assert.equal(getDashboardAccentColor("none", "warm"), undefined);
  assert.equal(getDashboardAccentColor("danger", "warm"), "#b91c1c");
  assert.equal(formatDashboardValue(1234.5, { ...defaults, numberFormat: "currency", currencyCode: "CAD", decimalPlaces: 2 }, "en-CA"), "$1,234.50");
  assert.equal(formatDashboardValue(92.5, { ...defaults, numberFormat: "percent", decimalPlaces: 1 }, "en-CA"), "92.5%");
  assert.equal(formatDashboardValue(1234.5, { ...defaults, displayUnit: "auto", decimalPlaces: 1 }, "en-CA"), "1.2K");
  assert.equal(formatDashboardValue(2_500_000, { ...defaults, numberFormat: "currency", displayUnit: "millions", currencyCode: "CAD", decimalPlaces: 1 }, "en-CA"), "$2.5M");
  assert.equal(formatDashboardValue(125_000, { ...defaults, numberFormat: "percent", displayUnit: "thousands", decimalPlaces: 0 }, "en-CA"), "125K%");
  assert.equal(formatDashboardCount(1_250_000, { ...defaults, numberFormat: "currency", displayUnit: "auto", decimalPlaces: 1 }, "en-CA"), "1.3M");
  assert.equal(formatDashboardValue(1_250_000, { ...defaults, numberFormat: "currency", displayUnit: "auto", currencyCode: "CAD", decimalPlaces: 1, valuePrefix: "~", valueSuffix: " kg" }, "en-CA"), "~$1.3M kg");
  assert.equal(formatDashboardCount(1250, { ...defaults, valuePrefix: "#", valueSuffix: " records" }, "en-CA"), "#1,250 records");
  assert.equal(isDashboardValueAffixValid("estimate: ", " kg"), true);
  assert.equal(isDashboardValueAffixValid("x".repeat(13), ""), false);
  assert.equal(isDashboardValueAffixValid("", "x".repeat(25)), false);
  assert.equal(isDashboardValueAffixValid("", "\nunsafe"), false);
  assert.equal(isDashboardValueAffixValid("\u202e", ""), false);
  assert.equal(defaults.showTooltips, true);
  assert.equal(defaults.tooltipContent, "auto");
  assert.equal(getDashboardTooltipText("auto", "QAQC", "Actual", "$1.4K", null), "Actual: $1.4K");
  assert.equal(getDashboardTooltipText("value", "QAQC", "Actual", "$1.4K", "52%"), "$1.4K (52%)");
  assert.equal(getDashboardTooltipText("category_value", "QAQC", "Actual", "$1.4K"), "QAQC: $1.4K");
  assert.equal(getDashboardTooltipText("series_category_value", "QAQC", "Actual", "$1.4K"), "Actual · QAQC: $1.4K");
  assert.equal(isDashboardTooltipContentValid("series_category_value"), true);
  assert.equal(isDashboardTooltipContentValid("html"), false);
});

test("KPI conditional formatting evaluates ordered bounded rules with a static fallback", () => {
  const appearance = resolveDashboardChartAppearance({ ...defaultDashboardChartAppearance, cardAccent: "info", conditionalFormatting: { enabled: true, rules: [
    { id: "excellent", operator: "greater_or_equal", value: 100, accent: "success", label: "On target" },
    { id: "near-target", operator: "greater_or_equal", value: 90, accent: "warning", label: "Watch" },
    { id: "below-target", operator: "less_than", value: 90, accent: "danger", label: "Below target" }
  ] } });
  assert.equal(getDashboardEffectiveCardAccent(appearance, 105), "success");
  assert.equal(getDashboardEffectiveCardAccent(appearance, 95), "warning");
  assert.equal(getDashboardEffectiveCardAccent(appearance, 70), "danger");
  assert.deepEqual(getDashboardConditionalResult(appearance, 95), { accent: "warning", label: "Watch", ruleId: "near-target" });
  assert.equal(getDashboardConditionalResult(appearance, 85)?.label, "Below target");
  assert.equal(getDashboardEffectiveCardAccent({ ...appearance, conditionalFormatting: { enabled: true, rules: [] } }, 70), "info");
  assert.equal(isDashboardConditionalFormattingValid(appearance.conditionalFormatting, "number_card"), true);
  assert.equal(isDashboardConditionalFormattingValid(appearance.conditionalFormatting, "choice_breakdown"), false);
  assert.equal(isDashboardConditionalFormattingValid({ enabled: true, rules: [{ id: "missing-label", operator: "equal", value: 1, accent: "success" }] }, "number_card"), false);
  assert.equal(isDashboardConditionalFormattingValid({ enabled: true, rules: [{ id: "bad", operator: "unsupported", value: 1, accent: "success" }] }, "number_card"), false);
  assert.equal(isDashboardConditionalFormattingValid({ enabled: true, rules: [{ id: "bad-label", operator: "equal", value: 1, accent: "success", label: "x".repeat(81) }] }, "number_card"), false);
  const cloned = cloneDashboardChartAppearance(appearance);
  cloned.conditionalFormatting.rules[0].value = 120;
  assert.equal(appearance.conditionalFormatting.rules[0].value, 100);
});

test("KPI targets format direction-aware outcome, progress, and variance", () => {
  const appearance = resolveDashboardChartAppearance({ ...defaultDashboardChartAppearance, numberFormat: "currency", currencyCode: "CAD", decimalPlaces: 0, kpiTarget: { enabled: true, value: 7000, label: "Monthly target", direction: "higher_is_better" } });
  assert.deepEqual(getDashboardKpiTargetSummary(appearance, 6651, "en-CA"), { label: "Monthly target", target: "$7,000", variance: "5.0% below target", outcome: "needs_attention", progress: 95 });
  assert.deepEqual(getDashboardKpiTargetSummary(appearance, 7350, "en-CA"), { label: "Monthly target", target: "$7,000", variance: "5.0% above target", outcome: "favorable", progress: 100 });
  assert.deepEqual(getDashboardKpiTargetSummary({ ...appearance, kpiTarget: { enabled: true, value: 0, label: "Zero incidents", direction: "lower_is_better" } }, 3, "en-CA"), { label: "Zero incidents", target: "$0", variance: "$3 above target", outcome: "needs_attention", progress: 0 });
  assert.deepEqual(getDashboardKpiTargetSummary({ ...appearance, kpiTarget: { enabled: true, value: 5, label: "Incident ceiling", direction: "lower_is_better" } }, 3, "en-CA"), { label: "Incident ceiling", target: "$5", variance: "40.0% below target", outcome: "favorable", progress: 100 });
  assert.deepEqual(getDashboardKpiTargetSummary(appearance, 7000, "en-CA"), { label: "Monthly target", target: "$7,000", variance: "On target", outcome: "on_target", progress: 100 });
  assert.equal(getDashboardKpiTargetSummary({ ...appearance, kpiTarget: { ...appearance.kpiTarget, value: -7000 } }, -6651, "en-CA")?.progress, null);
  assert.equal(getDashboardKpiTargetSummary({ ...appearance, kpiTarget: { ...appearance.kpiTarget, enabled: false } }, 6651, "en-CA"), null);
  assert.equal(isDashboardKpiTargetValid(appearance.kpiTarget, "number_card"), true);
  assert.equal(isDashboardKpiTargetValid(appearance.kpiTarget, "choice_breakdown"), false);
  assert.equal(isDashboardKpiTargetValid({ enabled: true, value: Number.POSITIVE_INFINITY, label: "Target" }, "number_card"), false);
  assert.equal(isDashboardKpiTargetValid({ enabled: true, value: 1, label: "" }, "number_card"), false);
  assert.equal(isDashboardKpiTargetValid({ enabled: true, value: 1, label: "x".repeat(81) }, "number_card"), false);
  assert.equal(isDashboardKpiTargetValid({ enabled: true, value: 1, label: "Target", direction: "unsupported" }, "number_card"), false);
  const cloned = cloneDashboardChartAppearance(appearance);
  cloned.kpiTarget.value = 8000;
  cloned.kpiTarget.label = "Changed target";
  assert.deepEqual(appearance.kpiTarget, { enabled: true, value: 7000, label: "Monthly target", direction: "higher_is_better" });
});

test("configured single series use the selected chart renderer outside KPI and table widgets", () => {
  const configured = [{ id: "actual", label: "Actual", metric: { type: "count" }, displayType: "area", color: "primary", axis: "left", points: [{ key: "2026-08", label: "Aug", value: 12 }] }];
  assert.equal(shouldRenderConfiguredSeriesChart({ widgetType: "trend", dataSeries: configured }), true);
  assert.equal(shouldRenderConfiguredSeriesChart({ widgetType: "breakdown", dataSeries: configured }), true);
  assert.equal(shouldRenderConfiguredSeriesChart({ widgetType: "summary", dataSeries: configured }), false);
  assert.equal(shouldRenderConfiguredSeriesChart({ widgetType: "table", dataSeries: configured }), false);
  assert.equal(shouldRenderConfiguredSeriesChart({ widgetType: "trend", dataSeries: [] }), false);
  assert.equal(shouldRenderConfiguredSeriesChart({ widgetType: "trend", dataSeries: [{ ...configured[0], points: [] }] }), false);
});

test("pie and donut displays are limited to one category-breakdown series", () => {
  const pie = { id: "records", label: "Records", metric: { type: "count" }, displayType: "pie", color: "primary", axis: "left" };
  const bar = { ...pie, id: "amount", label: "Amount", displayType: "bar" };
  assert.equal(isDashboardCircularDisplayType("pie"), true);
  assert.equal(isDashboardCircularDisplayType("donut"), true);
  assert.equal(isDashboardCircularDisplayType("area"), false);
  assert.equal(isDashboardSeriesPresentationValid("choice_breakdown", [pie]), true);
  assert.equal(isDashboardSeriesPresentationValid("date_trend", [pie]), false);
  assert.equal(isDashboardSeriesPresentationValid("choice_breakdown", [pie, bar]), false);
  assert.equal(isDashboardSeriesPresentationValid("choice_breakdown", [bar]), true);
});

test("circular chart segments preserve category proportions and reject negative values", () => {
  const segments = getDashboardCircularSegments([
    { key: "ready", label: "Ready", value: 30 },
    { key: "pending", label: "Pending", value: 10 },
    { key: "empty", label: "Empty", value: 0 }
  ]);
  assert.equal(segments.length, 2);
  assert.equal(segments[0].ratio, 0.75);
  assert.equal(segments[1].ratio, 0.25);
  assert.equal(segments[1].offset, 0.75);
  assert.deepEqual(getDashboardCircularSegments([{ key: "loss", label: "Loss", value: -1 }]), []);
});

test("reference lines are bounded, cartesian-only, cloned, and included in axis scaling", () => {
  const lines = [
    { id: "target", label: "Target", value: 100, color: "success", style: "dashed", axis: "left" },
    { id: "ceiling", label: "Ceiling", value: 250, color: "danger", style: "dotted", axis: "right" }
  ];
  const series = [{ id: "actual", label: "Actual", metric: { type: "count" }, displayType: "line", color: "primary", axis: "left", points: [{ key: "aug", label: "Aug", value: 80 }] }];
  assert.equal(isDashboardReferenceLinesValid(lines, "date_trend", series), true);
  assert.equal(isDashboardReferenceLinesValid(lines, "number_card", series), false);
  assert.equal(isDashboardReferenceLinesValid(lines, "choice_breakdown", [{ ...series[0], displayType: "pie" }]), false);
  assert.equal(isDashboardReferenceLinesValid([...lines, ...lines.map((line, index) => ({ ...line, id: `extra-${index}` }))], "date_trend", series), true);
  assert.equal(isDashboardReferenceLinesValid([...lines, { ...lines[0], id: "fifth-1" }, { ...lines[0], id: "fifth-2" }, { ...lines[0], id: "fifth-3" }], "date_trend", series), false);
  assert.equal(isDashboardReferenceLinesValid([{ ...lines[0], value: -1 }], "date_trend", series), false);
  assert.equal(getDashboardAxisMaximum(series, lines, "left"), 100);
  assert.equal(getDashboardAxisMaximum(series, lines, "right"), 250);
  const appearance = resolveDashboardChartAppearance({ ...defaultDashboardChartAppearance, referenceLines: lines });
  const clone = cloneDashboardChartAppearance(appearance);
  clone.referenceLines[0].value = 120;
  assert.equal(appearance.referenceLines[0].value, 100);
});

test("stacked bar modes require compatible series and calculate normal and percentage segments", () => {
  const series = [
    { id: "actual", label: "Actual", metric: { type: "count" }, displayType: "bar", color: "primary", axis: "left", points: [{ key: "aug", label: "Aug", value: 80 }] },
    { id: "target", label: "Target", metric: { type: "count" }, displayType: "bar", color: "success", axis: "left", points: [{ key: "aug", label: "Aug", value: 20 }] }
  ];
  assert.equal(isDashboardBarModeValid("grouped", "choice_breakdown", series, []), true);
  assert.equal(isDashboardBarModeValid("stacked", "choice_breakdown", series, []), true);
  assert.equal(isDashboardBarModeValid("stacked_percent", "date_trend", series, []), true);
  assert.equal(isDashboardBarModeValid("stacked", "number_card", series, []), false);
  assert.equal(isDashboardBarModeValid("stacked", "choice_breakdown", [{ ...series[0], displayType: "line" }, series[1]], []), false);
  assert.equal(isDashboardBarModeValid("stacked", "choice_breakdown", [series[0], { ...series[1], axis: "right" }], []), false);
  assert.equal(isDashboardBarModeValid("stacked_percent", "choice_breakdown", series, [{ id: "target", label: "Target", value: 80, color: "success", style: "dashed", axis: "left" }]), false);
  assert.deepEqual(getDashboardStackedBarSegment(series, "aug", 1, "stacked"), { start: 80, end: 100, percentage: 20, total: 100, value: 20 });
  assert.deepEqual(getDashboardStackedBarSegment(series, "aug", 1, "stacked_percent"), { start: 80, end: 100, percentage: 20, total: 100, value: 20 });
  assert.equal(getDashboardAxisMaximum(series, [], "left", "stacked"), 100);
  assert.equal(getDashboardAxisMaximum(series, [], "left", "stacked_percent"), 100);
  assert.equal(hasDashboardNegativeSeriesValues(series), false);
  assert.equal(hasDashboardNegativeSeriesValues([{ ...series[0], points: [{ key: "aug", label: "Aug", value: -1 }] }]), true);
});

test("horizontal bars are limited to compatible category series", () => {
  const series = [
    { id: "actual", label: "Actual", metric: { type: "count" }, displayType: "bar", color: "primary", axis: "left" },
    { id: "target", label: "Target", metric: { type: "count" }, displayType: "bar", color: "success", axis: "left" }
  ];
  assert.equal(defaultDashboardChartAppearance.barOrientation, "vertical");
  assert.equal(isDashboardBarOrientationValid("vertical", "date_trend", series), true);
  assert.equal(isDashboardBarOrientationValid("horizontal", "choice_breakdown", series), true);
  assert.equal(isDashboardBarOrientationValid("horizontal", "date_trend", series), false);
  assert.equal(isDashboardBarOrientationValid("horizontal", "choice_breakdown", [{ ...series[0], displayType: "line" }]), false);
  assert.equal(isDashboardBarOrientationValid("horizontal", "choice_breakdown", [series[0], { ...series[1], axis: "right" }]), false);
  assert.equal(isDashboardBarOrientationValid("diagonal", "choice_breakdown", series), false);
});

test("category sorting is breakdown-only and uses combined series values", () => {
  const series = [
    { points: [{ key: "b", label: "Beta", value: 10 }, { key: "a", label: "Alpha", value: 30 }, { key: "c", label: "Charlie", value: 20 }] },
    { points: [{ key: "b", label: "Beta", value: 25 }, { key: "a", label: "Alpha", value: 1 }, { key: "c", label: "Charlie", value: 5 }] }
  ];
  assert.equal(defaultDashboardChartAppearance.categorySort, "source");
  assert.equal(isDashboardCategorySortValid("value_desc", "choice_breakdown"), true);
  assert.equal(isDashboardCategorySortValid("value_desc", "date_trend"), false);
  assert.equal(isDashboardCategorySortValid("unsupported", "choice_breakdown"), false);
  assert.deepEqual(getDashboardOrderedCategoryKeys(series, "source"), ["b", "a", "c"]);
  assert.deepEqual(getDashboardOrderedCategoryKeys(series, "value_desc"), ["b", "a", "c"]);
  assert.deepEqual(getDashboardOrderedCategoryKeys(series, "value_asc"), ["c", "a", "b"]);
  assert.deepEqual(getDashboardOrderedCategoryKeys(series, "label_asc"), ["a", "b", "c"]);
  assert.deepEqual(getDashboardOrderedCategoryKeys(series, "label_desc"), ["c", "b", "a"]);
});

test("Top-N categories optionally aggregate the remaining returned points", () => {
  const series = [
    { id: "count", label: "Count", metric: { type: "count" }, displayType: "bar", color: "primary", axis: "left", points: [{ key: "a", label: "Alpha", value: 100 }, { key: "b", label: "Beta", value: 60 }, { key: "c", label: "Charlie", value: 20 }, { key: "d", label: "Delta", value: 10 }] },
    { id: "amount", label: "Amount", metric: { type: "sum", fieldId: "amount" }, displayType: "line", color: "success", axis: "right", points: [{ key: "a", label: "Alpha", value: 5 }, { key: "b", label: "Beta", value: 30 }, { key: "c", label: "Charlie", value: 40 }, { key: "d", label: "Delta", value: 5 }] }
  ];
  const result = getDashboardPresentedSeries(series, "value_desc", 2, true);
  assert.deepEqual(result.categoryKeys.slice(0, 2), ["a", "b"]);
  assert.equal(result.categoryKeys[2], result.aggregateKey);
  assert.deepEqual(result.series[0].points.map((point) => [point.label, point.value]), [["Alpha", 100], ["Beta", 60], ["Other", 30]]);
  assert.deepEqual(result.series[1].points.map((point) => [point.label, point.value]), [["Alpha", 5], ["Beta", 30], ["Other", 45]]);
  const limited = getDashboardPresentedSeries(series, "source", 2, false);
  assert.equal(limited.aggregateKey, null);
  assert.deepEqual(limited.categoryKeys, ["a", "b"]);
  assert.deepEqual(normalizeDashboardCategoryLimit(4, true, "choice_breakdown"), { limit: 4, groupRemaining: true });
  assert.deepEqual(normalizeDashboardCategoryLimit(4, true, "date_trend"), { limit: null, groupRemaining: false });
  const averageSeries = [{ metric: { type: "average", fieldId: "amount" } }];
  assert.deepEqual(normalizeDashboardCategoryLimit(4, true, "choice_breakdown", averageSeries), { limit: 4, groupRemaining: false });
  assert.equal(isDashboardCategoryLimitValid(4, true, "choice_breakdown", averageSeries), false);
  assert.equal(isDashboardCategoryLimitValid(12, false, "bar_chart"), true);
  assert.equal(isDashboardCategoryLimitValid(13, false, "choice_breakdown"), false);
  assert.equal(isDashboardCategoryLimitValid(null, true, "choice_breakdown"), false);
});

test("axis titles and manual maximums are bounded and normalized", () => {
  const series = [{ id: "actual", label: "Actual", metric: { type: "count" }, displayType: "bar", color: "primary", axis: "left" }];
  const axes = { left: { title: "Records", maximum: 75 }, right: { title: "", maximum: null } };
  assert.deepEqual(defaultDashboardChartAppearance.axes, { left: { title: "", maximum: null }, right: { title: "", maximum: null } });
  assert.equal(isDashboardChartAxesValid(axes, "choice_breakdown", series, [], "grouped"), true);
  assert.equal(isDashboardChartAxesValid({ ...axes, left: { ...axes.left, maximum: 0 } }, "choice_breakdown", series, [], "grouped"), false);
  assert.equal(isDashboardChartAxesValid({ ...axes, left: { ...axes.left, title: "x".repeat(81) } }, "choice_breakdown", series, [], "grouped"), false);
  assert.equal(isDashboardChartAxesValid(axes, "number_card", series, [], "grouped"), false);
  assert.deepEqual(normalizeDashboardChartAxes(axes, "choice_breakdown", series, [], "stacked_percent"), { left: { title: "Records", maximum: null }, right: { title: "", maximum: null } });
  assert.deepEqual(normalizeDashboardChartAxes(axes, "number_card", series, [], "grouped"), defaultDashboardChartAppearance.axes);
  assert.equal(resolveDashboardAxisMaximum(100, 75), 75);
  assert.equal(resolveDashboardAxisMaximum(100, null), 100);
  assert.equal(isDashboardAxisClipped(100, 75), true);
  assert.equal(isDashboardAxisClipped(75, 100), false);
  const resolved = resolveDashboardChartAppearance({ axes: { left: { title: "Amount", maximum: 500 }, right: { title: "", maximum: null } } });
  const clone = cloneDashboardChartAppearance(resolved);
  clone.axes.left.title = "Changed";
  assert.equal(resolved.axes.left.title, "Amount");
});

test("legend positions are bounded to chart widgets and default safely", () => {
  assert.equal(defaultDashboardChartAppearance.legendPosition, "top");
  assert.equal(resolveDashboardChartAppearance({}).legendPosition, "top");
  assert.equal(isDashboardLegendPositionValid("top", "number_card"), true);
  assert.equal(isDashboardLegendPositionValid("bottom", "choice_breakdown"), true);
  assert.equal(isDashboardLegendPositionValid("left", "date_trend"), true);
  assert.equal(isDashboardLegendPositionValid("right", "bar_chart"), true);
  assert.equal(isDashboardLegendPositionValid("right", "table"), false);
  assert.equal(isDashboardLegendPositionValid("floating", "choice_breakdown"), false);
});

test("data label content and placement are bounded and preserve legacy defaults", () => {
  const bars = [{ displayType: "bar" }, { displayType: "bar" }];
  assert.equal(defaultDashboardChartAppearance.dataLabelContent, "auto");
  assert.equal(defaultDashboardChartAppearance.dataLabelPosition, "auto");
  assert.deepEqual(normalizeDashboardDataLabelSettings("percentage", "inside", "choice_breakdown", bars, "stacked_percent"), { content: "percentage", position: "inside" });
  assert.deepEqual(normalizeDashboardDataLabelSettings("percentage", "outside", "choice_breakdown", bars, "grouped"), { content: "auto", position: "outside" });
  assert.deepEqual(normalizeDashboardDataLabelSettings("value_and_percentage", "inside", "choice_breakdown", [{ displayType: "donut" }], "grouped"), { content: "value_and_percentage", position: "auto" });
  assert.deepEqual(normalizeDashboardDataLabelSettings("value", "outside", "number_card", bars, "grouped"), { content: "auto", position: "auto" });
  assert.equal(isDashboardDataLabelSettingsValid("value_and_percentage", "outside", "choice_breakdown", bars, "stacked_percent"), true);
  assert.equal(isDashboardDataLabelSettingsValid("percentage", "auto", "date_trend", bars, "grouped"), false);
  assert.equal(getDashboardDataLabelText("auto", 25, null, String), "25");
  assert.equal(getDashboardDataLabelText("auto", 25, 12.5, String), "12.5%");
  assert.equal(getDashboardDataLabelText("value_and_percentage", 25, 12.5, String), "25 · 12.5%");
});

test("add-widget wizard filters visualizations, recommends compatible charts, and bounds recent choices", () => {
  const fields = [
    { id: "amount", label: "Amount", type: "currency", supportsAggregation: true, supportsChoiceGrouping: false },
    { id: "status", label: "Status", type: "status", supportsAggregation: false, supportsChoiceGrouping: true },
    { id: "created", label: "Created", type: "datetime", supportsAggregation: false, supportsChoiceGrouping: false }
  ];
  const availability = getVisualizationAvailability(fields);
  assert.equal(availability.breakdown.available, true);
  assert.equal(availability.trend.available, true);
  assert.deepEqual(filterDashboardVisualizations("line").map((item) => item.type), ["trend"]);
  assert.equal(getVisualizationAvailability(fields.slice(0, 1)).breakdown.available, false);

  let saved = "";
  const storage = { getItem: () => saved, setItem: (_key, value) => { saved = value; } };
  let recent = saveRecentDashboardVisualization(storage, "summary", []);
  recent = saveRecentDashboardVisualization(storage, "trend", recent);
  recent = saveRecentDashboardVisualization(storage, "table", recent);
  recent = saveRecentDashboardVisualization(storage, "breakdown", recent);
  assert.deepEqual(recent, ["breakdown", "table", "trend"]);
  assert.deepEqual(readRecentDashboardVisualizations(storage), recent);
});

test("canvas productivity helpers bound history, toggle selection, and enforce duplication limits", () => {
  assert.deepEqual(appendBoundedCanvasHistory([1, 2, 3], 4, 3), [2, 3, 4]);
  assert.deepEqual([...toggleDashboardWidgetSelection(new Set(["a"]), "a")], []);
  assert.deepEqual([...toggleDashboardWidgetSelection(new Set(["a"]), "b")], ["a", "b"]);
  const sections = [{ id: "one", title: "One", order: 0 }];
  const widgets = [{ id: "a", title: "A", sourceFormId: null, sectionId: "one" }];
  assert.equal(canDuplicateDashboardSection(sections, widgets, "one"), true);
  assert.equal(canDuplicateDashboardSection([sections[0], ...Array.from({ length: 15 }, (_, index) => ({ id: `s-${index}`, title: "S", order: index + 1 }))], widgets, "one"), false);
  assert.equal(canDuplicateDashboardSection(sections, Array.from({ length: 48 }, (_, index) => ({ id: `w-${index}`, title: "W", sourceFormId: null, sectionId: "one" })), "one"), false);
});

test("accessible canvas movement stays within sections and resolves adjacent sections", () => {
  const sections = [{ id: "one", title: "One", order: 0 }, { id: "two", title: "Two", order: 1 }];
  const widgets = [
    { id: "a", title: "A", sourceFormId: null, sectionId: "one" },
    { id: "b", title: "B", sourceFormId: null, sectionId: "two" },
    { id: "c", title: "C", sourceFormId: null, sectionId: "one" }
  ];
  const layout = [
    { id: "a", width: "small", order: 1 },
    { id: "b", width: "small", order: 2 },
    { id: "c", width: "small", order: 3 }
  ];
  assert.deepEqual(moveDashboardWidgetWithinSection(layout, widgets, "c", -1).map((item) => item.id), ["c", "b", "a"]);
  assert.equal(moveDashboardWidgetWithinSection(layout, widgets, "a", -1), layout);
  assert.equal(getAdjacentDashboardSectionId(sections, "one", 1), "two");
  assert.equal(getAdjacentDashboardSectionId(sections, "one", -1), null);
});

test("dashboard preview queue enforces the documented performance budget", async () => {
  let active = 0;
  let peak = 0;
  const completed = [];
  await runDashboardTasksWithConcurrency(Array.from({ length: 10 }, (_, index) => index), async (item) => {
    active += 1;
    peak = Math.max(peak, active);
    await new Promise((resolve) => setTimeout(resolve, 2));
    completed.push(item);
    active -= 1;
  });
  assert.equal(dashboardCanvasQualityLimits.maxWidgets, 48);
  assert.equal(dashboardCanvasQualityLimits.maxSections, 16);
  assert.equal(peak, dashboardCanvasQualityLimits.previewConcurrency);
  assert.equal(completed.length, 10);
});

test("dashboard analytics helpers preserve saved chart compatibility", () => {
  const chart = buildChartConfigFromDashboardAnalytics({
    widgetType: "breakdown",
    metricType: "average",
    metricFieldId: "salary",
    groupByFieldId: "status",
    dateFieldId: "created_at",
    columns: ["employee_name", "status"],
    limit: 25,
    reportId: "report-1"
  });

  assert.equal(chart.widgetType, "choice_breakdown");
  assert.deepEqual(chart.metric, { type: "average", fieldId: "salary" });
  assert.equal(chart.groupByFieldId, "status");
  assert.equal(chart.dateFieldId, null);
  assert.deepEqual(chart.columns, []);
  assert.equal(chart.limit, 25);
  assert.equal(chart.reportId, "report-1");

  const request = buildDashboardAnalyticsRequest("form-1", chart);

  assert.equal(request.widgetType, "breakdown");
  assert.deepEqual(request.source, { formId: "form-1", reportId: "report-1" });
  assert.deepEqual(request.metric, { type: "average", fieldId: "salary" });
  assert.equal(request.groupByFieldId, "status");
  assert.equal(request.dateFieldId, null);
  assert.deepEqual(request.columns, []);
  assert.equal(request.dateGranularity, "day");

  const trend = buildChartConfigFromDashboardAnalytics({
    widgetType: "trend",
    metricType: "count",
    dateFieldId: "created_at",
    dateGranularity: "quarter"
  });
  assert.equal(trend.dateGranularity, "quarter");
  assert.equal(buildDashboardAnalyticsRequest("form-1", trend).dateGranularity, "quarter");
});

test("dashboard analytics requests preserve fixed-filter precedence", () => {
  const chart = {
    widgetType: "number_card",
    metric: { type: "count", fieldId: null },
    columns: [],
    limit: 10,
    reportId: null,
    fixedFilters: [
      { fieldId: "module", values: ["Loss"] },
      { fieldId: "period_date", start: "2026-01-01", end: "2027-01-01" }
    ]
  };

  const request = buildDashboardAnalyticsRequest("form-1", chart, [
    { fieldId: "module", values: ["Production"] },
    { fieldId: "fiscal_year", values: ["2026"] }
  ]);

  assert.deepEqual(request.filters, [
    { fieldId: "module", values: ["Loss"] },
    { fieldId: "period_date", start: "2026-01-01", end: "2027-01-01" },
    { fieldId: "fiscal_year", values: ["2026"] }
  ]);
  request.filters[0].values[0] = "Changed";
  assert.equal(chart.fixedFilters[0].values[0], "Loss");
});

test("dashboard analytics helpers reject incomplete builder configs", () => {
  assert.equal(
    hasRequiredDashboardAnalyticsConfig({
      widgetType: "summary",
      metricType: "sum",
      metricFieldId: "",
      groupByFieldId: "status",
      dateFieldId: "created_at",
      columns: ["employee_name"],
      limit: 10,
      reportId: null
    }),
    false
  );
  assert.equal(
    hasRequiredDashboardAnalyticsConfig({
      widgetType: "breakdown",
      metricType: "count",
      metricFieldId: "",
      groupByFieldId: "",
      dateFieldId: "created_at",
      columns: ["employee_name"],
      limit: 10,
      reportId: null
    }),
    false
  );
  assert.equal(
    hasRequiredDashboardAnalyticsConfig({
      widgetType: "table",
      metricType: "count",
      metricFieldId: "",
      groupByFieldId: "status",
      dateFieldId: "created_at",
      columns: [],
      limit: 10,
      reportId: null
    }),
    false
  );
});

test("dashboard viewer helpers create independent preview states", () => {
  const widgets = [
    {
      id: "widget-1",
      title: "Record count",
      sourceFormId: "form-1",
      chart: {
        widgetType: "number_card",
        metric: { type: "count", fieldId: null },
        groupByFieldId: null,
        dateFieldId: null,
        columns: [],
        limit: 10,
        reportId: null
      }
    },
    {
      id: "widget-2",
      title: "Status",
      sourceFormId: "form-1",
      chart: {
        widgetType: "choice_breakdown",
        metric: { type: "count", fieldId: null },
        groupByFieldId: "status",
        dateFieldId: null,
        columns: [],
        limit: 10,
        reportId: null
      }
    }
  ];

  const states = createDashboardPreviewStates(widgets);

  assert.deepEqual(Object.keys(states), ["widget-1", "widget-2"]);
  assert.equal(states["widget-1"].status, "loading");
  assert.equal(states["widget-2"].status, "loading");
  assert.equal(states["widget-1"].error, undefined);
});

test("dashboard viewer helpers label V7 widget types", () => {
  assert.equal(getDashboardAnalyticsWidgetLabel("summary"), "Summary");
  assert.equal(getDashboardAnalyticsWidgetLabel("breakdown"), "Breakdown");
  assert.equal(getDashboardAnalyticsWidgetLabel("trend"), "Trend");
  assert.equal(getDashboardAnalyticsWidgetLabel("table"), "Table");
});

test("dashboard settings helpers normalize visibility defaults", () => {
  const emptySettings = { visibility: "workspace", isDefault: false, viewerUserIds: [], viewerRoleIds: [], viewerGroupIds: [] };
  assert.deepEqual(normalizeDashboardSettings(null), emptySettings);
  assert.deepEqual(normalizeDashboardSettings({ visibility: "private", isDefault: true, viewerUserIds: ["user-1"] }), { ...emptySettings, visibility: "private" });
  assert.deepEqual(normalizeDashboardSettings({ visibility: "workspace", isDefault: true, viewerRoleIds: ["role-1", "role-1"] }), { ...emptySettings, isDefault: true, viewerRoleIds: ["role-1"] });
  assert.equal(getDashboardVisibilityLabel("workspace"), "Workspace");
  assert.equal(getDashboardVisibilityLabel("private"), "Private");
});
