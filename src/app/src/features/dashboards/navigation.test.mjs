import assert from "node:assert/strict";
import { test } from "vitest";
import { applyDashboardNavigation, resolveDashboardIcon } from "./navigation.ts";

test("published dashboard navigation is nested under the directory in API order", () => {
  const navigation = applyDashboardNavigation(
    [{ label: "Dashboards", path: "/dashboards" }, { label: "Forms", path: "/forms" }],
    [
      { id: "2", slug: "team-overview", label: "Team overview", icon: "factory", order: 30 },
      { id: "1", slug: "executive-summary", label: "Executive summary", icon: "landmark", order: 20 }
    ]
  );
  assert.deepEqual(navigation[0].children.map((item) => item.path), ["/dashboards", "/dashboards/team-overview", "/dashboards/executive-summary"]);
  assert.equal(typeof navigation[0].children[1].icon, "object");
  assert.equal(resolveDashboardIcon("arbitrary-component"), undefined);
  assert.equal(navigation[1].path, "/forms");
});
