import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";

const outDir = "/tmp/obp-platform-module-registry-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/platform/moduleRegistry.ts",
    "--ignoreConfig",
    "--target",
    "ES2022",
    "--module",
    "ES2022",
    "--moduleResolution",
    "Bundler",
    "--outDir",
    outDir,
    "--skipLibCheck"
  ],
  { stdio: "inherit" }
);

const emittedRegistryPath = existsSync(`${outDir}/platform/moduleRegistry.js`)
  ? `${outDir}/platform/moduleRegistry.js`
  : `${outDir}/moduleRegistry.js`;
const registry = await import(`${emittedRegistryPath}?cache=${Date.now()}`);

const modules = [
  {
    id: "app.finance-dashboard",
    name: "Finance Dashboard",
    owner: "app",
    order: 30,
    navigation: [{ label: "Finance", path: "/finance-dashboard", order: 30 }],
    routes: [{ path: "/finance-dashboard", element: "finance" }]
  },
  {
    id: "core.dashboard",
    name: "Dashboard",
    owner: "core",
    order: 10,
    navigation: [{ label: "Dashboard", path: "/dashboard", order: 10 }],
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
  registry.getModuleRoutes(modules).map((route) => route.path ?? "index"),
  ["index", "/dashboard", "/finance-dashboard"]
);

assert.deepEqual(
  registry.getModulesByOwner(modules, "app").map((module) => module.id),
  ["app.finance-dashboard"]
);
