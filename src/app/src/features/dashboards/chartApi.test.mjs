import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";

test("chart API client maps preview requests and errors", async () => {
  const calls = [];
  const request = {
    widgetType: "bar_chart",
    metric: { type: "count", fieldId: null },
    groupByFieldId: "department",
    dateFieldId: null,
    columns: [],
    limit: 5,
    reportId: null
  };
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/forms/form-2/chart-widgets/preview" && init.method === "POST") {
      return {
        ok: true,
        json: async () => ({
          formId: "form-2",
          formName: "Employee information",
          widgetType: "bar_chart",
          metric: request.metric,
          columns: [],
          series: [{ key: "hr", label: "Human Resources", value: 2 }],
          rows: [],
          totalCount: 2
        })
      };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const result = await api.previewChartWidget("form-2", request, fetcher);

  assert.equal(result.widgetType, "bar_chart");
  assert.equal(result.series[0].label, "Human Resources");
  assert.equal(calls[0].input, "/api/forms/form-2/chart-widgets/preview");
  assert.equal(calls[0].init.method, "POST");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[0].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[0].init.body), request);

  await assert.rejects(
    () =>
      api.previewChartWidget("form-2", request, async () => ({
        ok: false,
        json: async () => ({ message: "Chart config is invalid." })
      })),
    (error) => {
      assert.equal(error.name, "DashboardApiError");
      assert.equal(error.message, "Chart config is invalid.");
      return true;
    }
  );
});
