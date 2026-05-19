import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-auth-client-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/auth/types.ts",
    "src/features/auth/authClient.ts",
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

const emittedClientPath = existsSync(`${outDir}/features/auth/authClient.js`)
  ? `${outDir}/features/auth/authClient.js`
  : `${outDir}/authClient.js`;
const require = createRequire(import.meta.url);
const { getCurrentUser, login, logout } = require(emittedClientPath);

const loginCalls = [];
const loginResult = await login(
  { email: "admin@company.test", password: "correct-password" },
  async (input, init) => {
    loginCalls.push({ input, init });
    return {
      ok: true,
      json: async () => ({
        user: {
          id: "bootstrap-admin",
          name: "Platform Admin",
          email: "admin@company.test",
          roles: ["Admin"],
          permissions: ["menu.forms", "forms.create"]
        }
      })
    };
  }
);

assert.deepEqual(loginResult, {
  id: "bootstrap-admin",
  name: "Platform Admin",
  email: "admin@company.test",
  roles: ["Admin"],
  permissions: ["menu.forms", "forms.create"]
});
assert.equal(loginCalls[0].input, "/api/auth/login");
assert.equal(loginCalls[0].init.method, "POST");
assert.equal(loginCalls[0].init.credentials, "include");
assert.equal(loginCalls[0].init.headers["Content-Type"], "application/json");
assert.deepEqual(JSON.parse(loginCalls[0].init.body), {
  email: "admin@company.test",
  password: "correct-password"
});

await assert.rejects(
  () =>
    login({ email: "admin@company.test", password: "bad-password" }, async () => ({
      ok: false,
      json: async () => ({ message: "Invalid email or password." })
    })),
  /Invalid email or password\./
);

const currentUser = await getCurrentUser(async () => ({
  ok: true,
  json: async () => ({
    user: {
      id: "bootstrap-admin",
      name: "Platform Admin",
      email: "admin@company.test",
      roles: ["Admin"],
      permissions: ["menu.users_access"]
    }
  })
}));

assert.equal(currentUser?.email, "admin@company.test");
assert.deepEqual(currentUser?.permissions, ["menu.users_access"]);

const anonymousUser = await getCurrentUser(async () => ({
  ok: false,
  status: 401,
  json: async () => ({ message: "Authentication required." })
}));

assert.equal(anonymousUser, null);

const logoutCalls = [];
await logout(async (input, init) => {
  logoutCalls.push({ input, init });
  return { ok: true, json: async () => ({}) };
});

assert.equal(logoutCalls[0].input, "/api/auth/logout");
assert.equal(logoutCalls[0].init.method, "POST");
assert.equal(logoutCalls[0].init.credentials, "include");
