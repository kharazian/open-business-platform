import assert from "node:assert/strict";
import { test } from "vitest";
import { completePasswordReset, getCurrentUser, login, logout, requestPasswordReset } from "./authClient.ts";

test("auth client maps login, session, logout, and auth errors", async () => {
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

  const customRoleLoginResult = await login(
    { email: "operations.manager@company.test", password: "temporary-password-1" },
    async () => ({
      ok: true,
      json: async () => ({
        user: {
          id: "11111111-1111-1111-1111-111111111111",
          name: "Operations Manager",
          email: "operations.manager@company.test",
          roles: ["Operations Manager"],
          permissions: ["menu.dashboard"]
        }
      })
    })
  );

  assert.deepEqual(customRoleLoginResult.roles, ["Operations Manager"]);
  assert.deepEqual(customRoleLoginResult.permissions, ["menu.dashboard"]);

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
});

test("auth client maps forgot and reset password requests", async () => {
  const forgotCalls = [];
  await requestPasswordReset("jane@company.test", async (input, init) => {
    forgotCalls.push({ input, init });
    return {
      ok: true,
      json: async () => ({
        message: "If the email belongs to an active user, a password reset link will be sent."
      })
    };
  });

  assert.equal(forgotCalls[0].input, "/api/auth/forgot-password");
  assert.equal(forgotCalls[0].init.method, "POST");
  assert.equal(forgotCalls[0].init.credentials, "include");
  assert.equal(forgotCalls[0].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(forgotCalls[0].init.body), {
    email: "jane@company.test"
  });

  const resetCalls = [];
  await completePasswordReset(
    { token: "raw-reset-token", newPassword: "new-temporary-password-2" },
    async (input, init) => {
      resetCalls.push({ input, init });
      return { ok: true, json: async () => ({}) };
    }
  );

  assert.equal(resetCalls[0].input, "/api/auth/reset-password");
  assert.equal(resetCalls[0].init.method, "POST");
  assert.equal(resetCalls[0].init.credentials, "include");
  assert.equal(resetCalls[0].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(resetCalls[0].init.body), {
    token: "raw-reset-token",
    newPassword: "new-temporary-password-2"
  });

  await assert.rejects(
    () =>
      completePasswordReset(
        { token: "bad-token", newPassword: "new-temporary-password-2" },
        async () => ({
          ok: false,
          json: async () => ({ message: "Reset token is invalid or expired." })
        })
      ),
    /Reset token is invalid or expired\./
  );
});
