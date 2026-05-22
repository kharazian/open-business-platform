import assert from "node:assert/strict";
import { test } from "vitest";
import { accessManagementTabs, departmentStatuses, isUserStatus, roleStatuses, userStatuses } from "./types.ts";

test("user access types expose active/inactive statuses and management tabs", () => {
  assert.deepEqual(accessManagementTabs.map((tab) => tab.label), ["Users", "Roles & permissions"]);
  assert.equal(accessManagementTabs.some((tab) => tab.value === "permissions"), false);
  assert.deepEqual(userStatuses, ["active", "inactive"]);
  assert.deepEqual(roleStatuses, ["active", "inactive"]);
  assert.deepEqual(departmentStatuses, ["active", "inactive"]);
  assert.equal(isUserStatus("active"), true);
  assert.equal(isUserStatus("invited"), false);
});
