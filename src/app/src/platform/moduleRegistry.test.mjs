import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

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
    "CommonJS",
    "--moduleResolution",
    "Node",
    "--ignoreDeprecations",
    "6.0",
    "--outDir",
    outDir,
    "--skipLibCheck",
    "--strict"
  ],
  { stdio: "inherit" }
);

const emittedRegistryPath = existsSync(`${outDir}/platform/moduleRegistry.js`)
  ? `${outDir}/platform/moduleRegistry.js`
  : `${outDir}/moduleRegistry.js`;
const require = createRequire(import.meta.url);
const registry = require(emittedRegistryPath);

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
