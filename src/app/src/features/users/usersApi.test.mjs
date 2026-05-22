import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";
import { formAccessActions, menuPermissionOptions } from "./types.ts";

test("users API client maps access requests and permission helpers", async () => {
  assert.deepEqual(formAccessActions, ["submit", "view", "edit", "delete", "manage"]);
  assert.equal(menuPermissionOptions.some((option) => option.value === "menu.users_access"), true);

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
});
