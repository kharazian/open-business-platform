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
  hasRequiredDashboardAnalyticsConfig
} from "./analytics.ts";
import { getDashboardWidgetGridClass, moveDashboardLayoutWidget, orderDashboardLayoutWidgets } from "./layout.ts";
import { cloneDashboardWidgetForEditing, isDashboardAnalyticsWidgetDraftValid } from "./components/DashboardWidgetPropertiesDrawer.tsx";
import { defaultDashboardChartAppearance, formatDashboardValue, getDashboardAccentColor, getDashboardSeriesColor, resolveDashboardChartAppearance } from "./appearance.ts";
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

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const summaries = await api.listDashboards(fetcher);
  const detail = await api.getDashboard("dash-1", fetcher);
  const created = await api.createDashboard(request, fetcher);
  const updated = await api.updateDashboard("dash-1", { ...request, concurrencyStamp: "stamp-1" }, fetcher);
  await api.deleteDashboard("dash-1", "stamp-2", fetcher);

  assert.equal(summaries[0].widgetCount, 1);
  assert.equal(summaries[0].visibility, "workspace");
  assert.equal(summaries[0].isDefault, true);
  assert.equal(detail.config.widgets[0].title, "Employees by department");
  assert.equal(detail.visibility, "workspace");
  assert.equal(detail.isDefault, true);
  assert.equal(created.id, "dash-1");
  assert.equal(updated.concurrencyStamp, "stamp-2");
  assert.equal(calls[0].input, "/api/dashboards");
  assert.equal(calls[2].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[2].init.body), request);
  assert.equal(calls.at(-1).init.method, "DELETE");
  assert.deepEqual(JSON.parse(calls.at(-1).init.body), { concurrencyStamp: "stamp-2" });

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
  const widget = { id: "widget-1", title: "Amount", sourceFormId: "form-1", sectionId: "overview", chart: { widgetType: "choice_breakdown", metric: { type: "sum", fieldId: "amount" }, groupByFieldId: "status", columns: [], limit: 10, series: [{ id: "amount", label: "Amount", metric: { type: "sum", fieldId: "amount" }, displayType: "bar", color: "primary", axis: "left" }], appearance: { ...defaultDashboardChartAppearance, palette: "warm", cardAccent: "warning" } } };
  const draft = cloneDashboardWidgetForEditing(widget);
  draft.chart.metric.fieldId = "other";
  draft.chart.series[0].metric.fieldId = "other";
  draft.chart.appearance.palette = "mono";
  assert.equal(widget.chart.metric.fieldId, "amount");
  assert.equal(widget.chart.series[0].metric.fieldId, "amount");
  assert.equal(widget.chart.appearance.palette, "warm");
  const fields = [
    { id: "amount", label: "Amount", type: "currency", source: "form", options: [], filterable: true, sortable: true, searchable: false, supportsAggregation: true, supportsChoiceGrouping: false },
    { id: "status", label: "Status", type: "status", source: "system", options: [], filterable: true, sortable: true, searchable: true, supportsAggregation: false, supportsChoiceGrouping: true }
  ];
  assert.equal(isDashboardAnalyticsWidgetDraftValid(widget, fields), true);
  assert.equal(isDashboardAnalyticsWidgetDraftValid({ ...widget, chart: { ...widget.chart, groupByFieldId: "hidden" } }, fields), false);
});

test("dashboard appearance helpers preserve defaults, palettes, accents, and localized formats", () => {
  const defaults = resolveDashboardChartAppearance(null);
  assert.deepEqual(defaults, defaultDashboardChartAppearance);
  assert.equal(getDashboardSeriesColor("primary", "cool"), "#2563eb");
  assert.equal(getDashboardAccentColor("none", "warm"), undefined);
  assert.equal(getDashboardAccentColor("danger", "warm"), "#b91c1c");
  assert.equal(formatDashboardValue(1234.5, { ...defaults, numberFormat: "currency", currencyCode: "CAD", decimalPlaces: 2 }, "en-CA"), "$1,234.50");
  assert.equal(formatDashboardValue(92.5, { ...defaults, numberFormat: "percent", decimalPlaces: 1 }, "en-CA"), "92.5%");
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
