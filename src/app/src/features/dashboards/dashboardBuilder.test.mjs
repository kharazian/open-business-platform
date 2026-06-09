import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";
import {
  buildChartConfigFromDashboardAnalytics,
  buildDashboardAnalyticsRequest,
  createDashboardPreviewStates,
  getDashboardAnalyticsWidgetLabel,
  hasRequiredDashboardAnalyticsConfig
} from "./analytics.ts";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "./layout.ts";

test("dashboard API client maps saved dashboard requests and errors", async () => {
  const calls = [];
  const request = {
    name: "Operations dashboard",
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
    }
  };
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/dashboards" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [{ id: "dash-1", name: "Operations dashboard", widgetCount: 1, concurrencyStamp: "stamp-1", createdAt: "2026-06-01T12:00:00.000Z" }]
        })
      };
    }

    if (input === "/api/dashboards/dash-1" && init.method === "GET") {
      return { ok: true, json: async () => ({ id: "dash-1", concurrencyStamp: "stamp-1", createdAt: "2026-06-01T12:00:00.000Z", ...request }) };
    }

    if (input === "/api/dashboards" && init.method === "POST") {
      return { ok: true, json: async () => ({ id: "dash-1", concurrencyStamp: "stamp-1", createdAt: "2026-06-01T12:00:00.000Z", ...request }) };
    }

    if (input === "/api/dashboards/dash-1" && init.method === "PUT") {
      return { ok: true, json: async () => ({ id: "dash-1", concurrencyStamp: "stamp-2", createdAt: "2026-06-01T12:00:00.000Z", ...request }) };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const summaries = await api.listDashboards(fetcher);
  const detail = await api.getDashboard("dash-1", fetcher);
  const created = await api.createDashboard(request, fetcher);
  const updated = await api.updateDashboard("dash-1", { ...request, concurrencyStamp: "stamp-1" }, fetcher);

  assert.equal(summaries[0].widgetCount, 1);
  assert.equal(detail.config.widgets[0].title, "Employees by department");
  assert.equal(created.id, "dash-1");
  assert.equal(updated.concurrencyStamp, "stamp-2");
  assert.equal(calls[0].input, "/api/dashboards");
  assert.equal(calls[2].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[2].init.body), request);

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
