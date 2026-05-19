import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-users-api-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/types/entities.ts",
    "src/features/users/types.ts",
    "src/features/users/api.ts",
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

const emittedApiPath = existsSync(`${outDir}/features/users/api.js`) ? `${outDir}/features/users/api.js` : `${outDir}/api.js`;
const emittedTypesPath = existsSync(`${outDir}/features/users/types.js`) ? `${outDir}/features/users/types.js` : `${outDir}/types.js`;
const require = createRequire(import.meta.url);
const api = require(emittedApiPath);
const types = require(emittedTypesPath);

assert.deepEqual(types.formAccessActions, ["submit", "view", "edit", "delete", "manage"]);
assert.equal(types.menuPermissionOptions.some((option) => option.value === "menu.users_access"), true);

const calls = [];
const fetcher = async (input, init = {}) => {
  calls.push({ input, init });
  if (input === "/api/users") return { ok: true, json: async () => ({ items: [] }) };
  if (input === "/api/users/user-1/reset-password") return { ok: true, json: async () => ({}) };
  if (input === "/api/roles/role-1/permissions") {
    return {
      ok: true,
      json: async () => ({
        roleId: "role-1",
        permissions: ["menu.forms"],
        formPermissions: [{ formId: "form-1", action: "view" }]
      })
    };
  }
  if (input === "/api/forms/access-options") return { ok: true, json: async () => ({ items: [{ id: "form-1", name: "Expense", status: "draft" }] }) };
  return { ok: true, json: async () => ({ id: "created" }) };
};

await api.listUsers(fetcher);
await api.createUser(
  {
    name: "Jane Cooper",
    email: "jane@company.test",
    password: "temporary-password-1",
    roleIds: ["role-1"],
    departmentIds: [],
    isActive: true
  },
  fetcher
);
await api.resetUserPassword("user-1", { newPassword: "new-temporary-password-2" }, fetcher);
const rolePermissions = await api.updateRolePermissions(
  "role-1",
  { permissions: ["menu.forms"], formPermissions: [{ formId: "form-1", action: "view" }] },
  fetcher
);
const formOptions = await api.listFormAccessOptions(fetcher);

assert.equal(calls[0].input, "/api/users");
assert.equal(calls[1].input, "/api/users");
assert.equal(calls[1].init.method, "POST");
assert.equal(JSON.parse(calls[1].init.body).password, "temporary-password-1");
assert.equal(calls[2].input, "/api/users/user-1/reset-password");
assert.equal(calls[2].init.method, "POST");
assert.equal(calls[3].input, "/api/roles/role-1/permissions");
assert.equal(calls[3].init.method, "PUT");
assert.equal(rolePermissions.formPermissions[0].action, "view");
assert.equal(formOptions[0].name, "Expense");
