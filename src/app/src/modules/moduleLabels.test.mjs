import assert from "node:assert/strict";
import { test } from "vitest";
import { dashboardModule } from "./dashboard/module.tsx";
import { notificationsModule } from "./notifications/module.tsx";
import { reportsModule } from "./reports/module.tsx";

test("reports module is labeled as a V2 preview in the finalized V1 app", () => {
  assert.equal(reportsModule.name, "Reports (V2 preview)");
  assert.equal(reportsModule.navigation[0].label, "Reports (V2 preview)");
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
