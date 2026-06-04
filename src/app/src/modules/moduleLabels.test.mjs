import assert from "node:assert/strict";
import { test } from "vitest";
import { dashboardModule } from "./dashboard/module.tsx";
import { notificationsModule } from "./notifications/module.tsx";
import { reportsModule } from "./reports/module.tsx";
import { workflowsModule } from "./workflows/module.tsx";

test("reports module uses finalized labels after V2 completion", () => {
  assert.equal(reportsModule.name, "Reports");
  assert.equal(reportsModule.navigation[0].label, "Reports");
  assert.equal(reportsModule.navigation[1].label, "Charts");
});

test("dashboard module exposes saved dashboards navigation", () => {
  assert.ok(dashboardModule.routes.some((route) => route.path === "/dashboards"));
  assert.ok(dashboardModule.navigation.some((item) => item.path === "/dashboards" && item.label === "Dashboards"));
});

test("notifications module exposes the current-user inbox route", () => {
  assert.equal(notificationsModule.id, "app.notifications");
  assert.ok(notificationsModule.routes.some((route) => route.path === "/notifications"));
  assert.ok(notificationsModule.navigation.some((item) => item.path === "/notifications" && item.label === "Notifications"));
});

test("workflows module exposes the V5 management workspace", () => {
  assert.equal(workflowsModule.id, "app.workflows");
  assert.ok(workflowsModule.routes.some((route) => route.path === "/workflows" && route.permission === "menu.forms"));
  assert.ok(workflowsModule.navigation.some((item) => item.path === "/workflows" && item.label === "Workflows"));
  assert.ok(workflowsModule.routes.some((route) => route.path === "/workflow-approvals"));
  assert.ok(workflowsModule.navigation.some((item) => item.path === "/workflow-approvals" && item.label === "Approvals"));
});
