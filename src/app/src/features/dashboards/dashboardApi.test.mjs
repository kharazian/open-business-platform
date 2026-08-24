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

test("dashboard publication API maps navigation, slug viewer, publish, and unpublish endpoints", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    if (input === "/api/dashboards/navigation") return { ok: true, json: async () => ({ items: [{ id: "1", slug: "team-overview", label: "Team overview", order: 30 }] }) };
    return { ok: true, json: async () => ({ id: "1", publication: { status: input.endsWith("/unpublish") ? "draft" : "published" } }) };
  };
  assert.equal((await api.listDashboardNavigation(fetcher))[0].slug, "team-overview");
  await api.getDashboardBySlug("team plan", fetcher);
  await api.publishDashboard("1", "stamp-1", fetcher);
  await api.unpublishDashboard("1", "stamp-2", fetcher);
  assert.deepEqual(calls.map((call) => call.input), [
    "/api/dashboards/navigation",
    "/api/dashboards/by-slug/team%20plan",
    "/api/dashboards/1/publish",
    "/api/dashboards/1/unpublish"
  ]);
  assert.equal(calls[2].init.method, "POST");
  assert.equal(calls[2].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[2].init.body), { concurrencyStamp: "stamp-1" });
  assert.deepEqual(JSON.parse(calls[3].init.body), { concurrencyStamp: "stamp-2" });
});

test("dashboard API errors retain structured validation details", async () => {
  await assert.rejects(
    () => api.listDashboards(async () => ({
      ok: false,
      json: async () => ({
        message: "Dashboard config is invalid.",
        errors: [{ path: "config.sections", code: "dashboard.section.invalid", message: "Each section needs a title." }]
      })
    })),
    (error) => {
      assert.equal(error.name, "DashboardApiError");
      assert.deepEqual(error.errors, [{ path: "config.sections", code: "dashboard.section.invalid", message: "Each section needs a title." }]);
      return true;
    }
  );
});

test("dashboard publishing history API maps comparison, revisions, and restore endpoints", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    if (input.endsWith("/revisions")) return { ok: true, json: async () => ({ items: [{ id: "revision/1", revisionNumber: 3, reason: "published", isPublished: true }] }) };
    if (input.endsWith("/published-comparison")) return { ok: true, json: async () => ({ hasPublishedVersion: true, published: { name: "Live dashboard" } }) };
    return { ok: true, json: async () => ({ id: "dashboard 1", name: "Restored dashboard" }) };
  };

  const revisions = await api.listDashboardRevisions("dashboard 1", fetcher);
  const comparison = await api.getDashboardPublishedComparison("dashboard 1", fetcher);
  await api.restoreDashboardRevision("dashboard 1", "revision/1", "stamp-3", fetcher);

  assert.equal(revisions[0].revisionNumber, 3);
  assert.equal(comparison.published.name, "Live dashboard");
  assert.deepEqual(calls.map((call) => call.input), [
    "/api/dashboards/dashboard%201/revisions",
    "/api/dashboards/dashboard%201/published-comparison",
    "/api/dashboards/dashboard%201/revisions/revision%2F1/restore"
  ]);
  assert.deepEqual(JSON.parse(calls[2].init.body), { concurrencyStamp: "stamp-3" });
});
