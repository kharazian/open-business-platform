import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";

test("dashboard API client maps summary requests and errors", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/dashboard/summary" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          title: "Open Business Platform",
          metrics: [
            { key: "users", label: "Users", value: 4 },
            { key: "forms", label: "Forms", value: 3 },
            { key: "records", label: "Records", value: 10 },
            { key: "reports", label: "Reports", value: 2 },
            { key: "audit_logs", label: "Audit logs", value: 7 }
          ],
          recentActivity: [
            {
              id: "activity-1",
              event: "Record created",
              actor: "Jane Cooper",
              createdAt: "2026-05-22T12:00:00.000Z",
              status: "Completed"
            }
          ]
        })
      };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const summary = await api.getDashboardSummary(fetcher);

  assert.equal(summary.metrics.find((metric) => metric.key === "records").value, 10);
  assert.equal(summary.recentActivity[0].event, "Record created");
  assert.equal(calls[0].input, "/api/dashboard/summary");
  assert.equal(calls[0].init.method, "GET");
  assert.equal(calls[0].init.credentials, "include");

  await assert.rejects(
    () =>
      api.getDashboardSummary(async () => ({
        ok: false,
        json: async () => ({ message: "Dashboard access denied." })
      })),
    (error) => {
      assert.equal(error.name, "DashboardApiError");
      assert.equal(error.message, "Dashboard access denied.");
      return true;
    }
  );
});
