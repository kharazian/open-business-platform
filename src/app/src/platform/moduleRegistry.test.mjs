import assert from "node:assert/strict";
import { test } from "vitest";
import * as registry from "./moduleRegistry.ts";

test("module registry sorts and filters navigation and routes", () => {
  const modules = [
    {
      id: "app.finance-dashboard",
      name: "Finance Dashboard",
      owner: "app",
      order: 30,
      navigation: [{ label: "Finance", path: "/finance-dashboard", order: 30, permission: "menu.finance" }],
      routes: [{ path: "/finance-dashboard", element: "finance", permission: "menu.finance" }]
    },
    {
      id: "core.dashboard",
      name: "Dashboard",
      owner: "core",
      order: 10,
      navigation: [{ label: "Dashboard", path: "/dashboard", order: 10, permission: "menu.dashboard" }],
      routes: [
        { index: true, element: "home" },
        { path: "/dashboard", element: "dashboard" }
      ]
    }
  ];

  assert.deepEqual(
    registry.getModuleNavigation(modules).map((item) => item.label),
    ["Dashboard", "Finance"]
  );

  assert.deepEqual(
    registry.filterNavigationByPermissions(registry.getModuleNavigation(modules), new Set(["menu.dashboard"])).map((item) => item.label),
    ["Dashboard"]
  );

  assert.deepEqual(
    registry.filterNavigationByPermissions(
      [
        {
          label: "Admin",
          children: [
            { label: "Users", path: "/users", permission: "menu.users_access" },
            { label: "Reports", path: "/reports", permission: "menu.reports" }
          ]
        }
      ],
      new Set(["menu.reports"])
    ),
    [{ label: "Admin", children: [{ label: "Reports", path: "/reports", permission: "menu.reports" }] }]
  );

  assert.deepEqual(
    registry.getModuleRoutes(modules).map((route) => route.path ?? "index"),
    ["index", "/dashboard", "/finance-dashboard"]
  );

  assert.deepEqual(
    registry.getModulesByOwner(modules, "app").map((module) => module.id),
    ["app.finance-dashboard"]
  );
});
