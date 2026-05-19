import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-user-types-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/types/entities.ts",
    "src/features/users/types.ts",
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

const emittedUserTypesPath = existsSync(`${outDir}/features/users/types.js`)
  ? `${outDir}/features/users/types.js`
  : `${outDir}/types.js`;
const require = createRequire(import.meta.url);
const { accessManagementTabs, userStatuses, roleStatuses, departmentStatuses, isUserStatus } = require(emittedUserTypesPath);

assert.deepEqual(accessManagementTabs.map((tab) => tab.label), ["Users", "Roles & permissions"]);
assert.equal(accessManagementTabs.some((tab) => tab.value === "permissions"), false);
assert.deepEqual(userStatuses, ["active", "inactive"]);
assert.deepEqual(roleStatuses, ["active", "inactive"]);
assert.deepEqual(departmentStatuses, ["active", "inactive"]);
assert.equal(isUserStatus("active"), true);
assert.equal(isUserStatus("invited"), false);
